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

    public ConsumptionService(Meteringpoint.V1.Meteringpoint.MeteringpointClient meteringPointClient,
        IDataHubFacadeClient dhFacadeClient,
        IMeasurementClient measurementClient)
    {
        _meteringPointClient = meteringPointClient;
        _dhFacadeClient = dhFacadeClient;
        _measurementClient = measurementClient;
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

        var consumptionGs = owned.MeteringPoints
            .Where(mp => IsConsumption(mp.TypeOfMp))
            .Select(mp => new Gsrn(mp.MeteringPointId))
            .ToList();

        var relations = await _dhFacadeClient.ListCustomerRelations(
            orgId.Value.ToString(),
            consumptionGs, ct);

        var valid = (relations?.Relations ?? Enumerable.Empty<CustomerRelation>())
            .Where(r => r.IsValid())
            .Select(r => new Gsrn(r.MeteringPointId))
            .ToList();

        var consumptionOnly = valid.Where(x => consumptionGs.Contains(x)).ToList().AsReadOnly();

        return consumptionOnly;
    }

    private static List<ConsumptionHour> ComputeHourlyAverages(MeasurementAggregationByPeriodDto[] data)
    {
        var allPoints = data
            .SelectMany(mp => mp.PointAggregationGroups.Values)
            .SelectMany(pg => pg.PointAggregations)
            .Select(pa => new
            {
                DateTimeOffset.FromUnixTimeSeconds(pa.From.ToUnixTimeSeconds()).Hour,
                DateTimeOffset.FromUnixTimeSeconds(pa.From.ToUnixTimeSeconds()).Date,
                Quantity = pa.Quantity ?? 0m
            });

        var groupedByDayHour = allPoints
            .GroupBy(x => new { x.Date, x.Hour })
            .Select(g => new
            {
                g.Key.Hour,
                DailySum = g.Sum(x => x.Quantity)
            });

        var averagesByHour = groupedByDayHour
            .GroupBy(x => x.Hour)
            .Select(g => new ConsumptionHour(g.Key)
            {
                KwhQuantity = g.Average(x => x.DailySum)
            })
            .ToList();

        var hourlyAverages = Enumerable.Range(0, 24)
            .Select(h => averagesByHour.FirstOrDefault(x => x.HourOfDay == h) ?? new ConsumptionHour(h))
            .ToList();

        return hourlyAverages;
    }

    private static bool IsConsumption(string typeOfMp)
    {
        return typeOfMp.Trim().Equals("E17", StringComparison.OrdinalIgnoreCase);
    }

    private List<ConsumptionHour> MapToTotalHourFormat(MeasurementAggregationByPeriodDto[] totalConsumption)
    {
        var result = Enumerable.Range(0, 24).Select(x => new ConsumptionHour(x)).ToList();

        foreach (var mp in totalConsumption)
        {
            foreach (var day in mp.PointAggregationGroups)
            {
                foreach (var entry in day.Value.PointAggregations)
                {
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
