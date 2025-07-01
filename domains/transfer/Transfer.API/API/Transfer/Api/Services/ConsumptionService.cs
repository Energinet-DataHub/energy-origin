using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api.Services;

public interface IConsumptionService
{
    Task<(List<ConsumptionHour>, List<ConsumptionHour>)> GetTotalAndAverageHourlyConsumption(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

public class ConsumptionService : IConsumptionService
{
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _meteringPointClient;
    private readonly IDataHubFacadeClient _dhFacadeClient;
    private readonly IMeasurementClient _measurementClient;
    private readonly ILogger<ConsumptionService> _logger;

    public ConsumptionService(Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointClient,
        IDataHubFacadeClient dhFacadeClient,
        IMeasurementClient measurementClient,
        ILogger<ConsumptionService> logger)
    {
        _meteringPointClient = meteringPointClient;
        _dhFacadeClient = dhFacadeClient;
        _measurementClient = measurementClient;
        _logger = logger;
    }

    public async Task<(List<ConsumptionHour>, List<ConsumptionHour>)> GetTotalAndAverageHourlyConsumption(OrganizationId orgId, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default)
    {
        var data = await GetRawMeteringDataAsync(orgId, from, to, ct);

        return (MapToTotalHourFormat(data), ComputeHourlyAverages(data));
    }

    private async Task<MeasurementAggregationByPeriodDto[]> GetRawMeteringDataAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var gsrns = await GetValidGsrnsAsync(orgId, ct);
        _logger.LogInformation("Gsrns " + string.Join(',', gsrns.Select(x => x.Value)));
        if (gsrns.Count == 0) return [];

        return (await _measurementClient.GetMeasurements(
                   [.. gsrns],
                   from.ToUnixTimeSeconds(),
                   to.ToUnixTimeSeconds(),
                   ct)).ToArray()
               ?? [];
    }

    private async Task<ReadOnlyCollection<Gsrn>> GetValidGsrnsAsync(
        OrganizationId orgId,
        CancellationToken ct)
    {
        var owned = await _meteringPointClient.GetOwnedMeteringPointsAsync(
            new OwnedMeteringPointsRequest { Subject = orgId.Value.ToString() },
            cancellationToken: ct);

        _logger.LogInformation("TypeOfMps: " + string.Join(',', owned.MeteringPoints.Select(x => x.TypeOfMp)));

        var consumptionGs = owned.MeteringPoints
            .Where(mp => IsConsumption(mp.TypeOfMp))
            .Select(mp => new Gsrn(mp.MeteringPointId))
            .ToList();

        _logger.LogInformation("consumptionGs: " + string.Join(',', consumptionGs.Select(x => x.Value)));

        var relations = await _dhFacadeClient.ListCustomerRelations(
            orgId.Value.ToString(),
            consumptionGs, ct);

        var valid = (relations?.Relations ?? Enumerable.Empty<CustomerRelation>())
            .Where(r => r.IsValid())
            .Select(r => new Gsrn(r.MeteringPointId))
            .ToList();

        _logger.LogInformation("valid: " + string.Join(',', valid.Select(x => x.Value)));

        var consumptionOnly = valid.Where(x => consumptionGs.Contains(x)).ToList().AsReadOnly();

        _logger.LogInformation("consumptionOnly: " + string.Join(',', consumptionOnly.Select(x => x.Value)));
        return consumptionOnly;
    }

    private List<ConsumptionHour> ComputeHourlyAverages(MeasurementAggregationByPeriodDto[] data)
    {
        return Enumerable.Range(0, 24)
            .Select(hour => new ConsumptionHour(hour)
            {
                KwhQuantity = data
                    .SelectMany(mp => mp.PointAggregationGroups.Values)
                    .SelectMany(pg => pg.PointAggregations)
                    .Where(p => DateTimeOffset
                        .FromUnixTimeSeconds(p.From.ToUnixTimeSeconds())
                        .Hour == hour)
                    .Select(p => p.Quantity ?? 0)
                    .DefaultIfEmpty(0m)
                    .Average()
            })
            .ToList();
    }

    private static bool IsConsumption(string typeOfMp)
    {
        return typeOfMp.Trim().Equals("E17", StringComparison.OrdinalIgnoreCase);
    }

    //TODO Test this
    private List<ConsumptionHour> MapToTotalHourFormat(MeasurementAggregationByPeriodDto[] totalConsumption)
    {
        var result = Enumerable.Range(0, 24).Select(x => new ConsumptionHour(x)).ToList();

        foreach (var mp in totalConsumption)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                foreach (var entry in day.Value.PointAggregations)
                {
                    _logger.LogInformation("Entry: " + (entry.Quantity ?? 0));
                    var hour = DateTimeOffset.FromUnixTimeSeconds(entry.From.ToUnixTimeSeconds()).Hour;

                    result.First(x => x.HourOfDay == hour).KwhQuantity += entry.Quantity ?? 0;
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
