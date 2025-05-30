using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;

namespace API.Transfer.Api.Services;

public interface IConsumptionService
{
    Task<List<ConsumptionHour>> GetTotalHourlyConsumption(OrganizationId organizationId, DateTimeOffset dateFrom, DateTimeOffset dateTo, CancellationToken cancellationToken);
    Task<List<ConsumptionHour>> GetAverageHourlyConsumption(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

public class ConsumptionService : IConsumptionService
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClient;
    private readonly IDataHubFacadeClient _dhFacadeClient;
    private readonly IDataHub3Client _dh3Client;

    public ConsumptionService(Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointClient,
        IDataHubFacadeClient dhFacadeClient,
        IDataHub3Client dh3Client)
    {
        _meteringPointClient = meteringPointClient;
        _dhFacadeClient = dhFacadeClient;
        _dh3Client = dh3Client;
    }

    public async Task<List<ConsumptionHour>> GetTotalHourlyConsumption(OrganizationId organizationId, DateTimeOffset dateFrom, DateTimeOffset dateTo, CancellationToken cancellationToken)
    {
        var mpsRequest = new OwnedMeteringPointsRequest
        {
            Subject = organizationId.Value.ToString()
        };

        var mps = await _meteringPointClient.GetOwnedMeteringPointsAsync(mpsRequest, cancellationToken: cancellationToken);

        var consumptionGsrns = mps.MeteringPoints
            .Where(x => IsConsumption(x.TypeOfMp))
            .Select(x => new Gsrn(x.MeteringPointId))
            .ToList();

        var mpRelations = await _dhFacadeClient.ListCustomerRelations(organizationId.Value.ToString(), consumptionGsrns, cancellationToken);
        if (mpRelations == null)
            return new List<ConsumptionHour>();

        var validRelations = mpRelations.Relations.Where(x => x.IsValid()).ToList();

        var totalConsumption = await _dh3Client.GetMeasurements(validRelations.Select(x => new Gsrn(x.MeteringPointId)).ToList(), dateFrom.ToUnixTimeSeconds(), dateTo.ToUnixTimeSeconds(), cancellationToken);

        if (totalConsumption == null)
            return new List<ConsumptionHour>();

        return MapToTotalHourFormat(totalConsumption);
    }

    public async Task<List<ConsumptionHour>> GetAverageHourlyConsumption(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var data = await GetRawMeteringDataAsync(orgId, from, to, ct);
        return ComputeHourlyAverages(data);
    }

    private async Task<MeteringPointData[]> GetRawMeteringDataAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var gsrns = await GetValidGsrnsAsync(orgId, ct);
        if (gsrns.Count == 0) return Array.Empty<MeteringPointData>();

        return await _dh3Client.GetMeasurements(
                   gsrns.ToList(),
                   from.ToUnixTimeSeconds(),
                   to.ToUnixTimeSeconds(),
                   ct)
               ?? Array.Empty<MeteringPointData>();
    }


    private async Task<ReadOnlyCollection<Gsrn>> GetValidGsrnsAsync(
        OrganizationId orgId,
        CancellationToken ct)
    {
        var owned = await _meteringPointClient.GetOwnedMeteringPointsAsync(
            new OwnedMeteringPointsRequest { Subject = orgId.Value.ToString() },
            cancellationToken: ct);

        var consumptionGs = owned.MeteringPoints
            .Where(mp => mp.TypeOfMp.Trim().Equals("E17", StringComparison.OrdinalIgnoreCase))
            .Select(mp => new Gsrn(mp.MeteringPointId))
            .ToList();

        var relations = await _dhFacadeClient.ListCustomerRelations(
            orgId.Value.ToString(),
            consumptionGs, ct);

        var valid = (relations?.Relations ?? Enumerable.Empty<CustomerRelation>())
            .Where(r => r.IsValid())
            .Select(r => new Gsrn(r.MeteringPointId))
            .ToList()
            .AsReadOnly();

        return valid;
    }

    private List<ConsumptionHour> ComputeHourlyAverages(MeteringPointData[] data)
    {
        return Enumerable.Range(0, 24)
            .Select(hour => new ConsumptionHour(hour)
            {
                KwhQuantity = data
                    .SelectMany(mp => mp.PointAggregationGroups.Values)
                    .SelectMany(pg => pg.PointAggregations)
                    .Where(p => DateTimeOffset
                        .FromUnixTimeSeconds(p.MinObservationTime)
                        .Hour == hour)
                    .Select(p => p.AggregatedQuantity)
                    .DefaultIfEmpty(0m)
                    .Average()
            })
            .ToList();
    }

    private static bool IsConsumption(string typeOfMp)
    {
        return typeOfMp.Trim().ToUpper() == "E17";
    }

    private List<ConsumptionHour> MapToTotalHourFormat(MeteringPointData[] totalConsumption)
    {
        var result = Enumerable.Range(0, 24).Select(x => new ConsumptionHour(x)).ToList();

        foreach (var mp in totalConsumption)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                foreach (var entry in day.Value.PointAggregations)
                {
                    var hour = DateTimeOffset.FromUnixTimeSeconds(entry.MinObservationTime).Hour;

                    result.First(x => x.HourOfDay == hour).KwhQuantity += entry.AggregatedQuantity;
                }
            }
        }

        return result;
    }
}

public record ConsumptionHour(int HourOfDay)
{
    public decimal KwhQuantity { get; set; }
}
