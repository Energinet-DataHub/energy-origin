using API.Configuration;
using API.Emissions.Models;
using API.Extensions;
using API.Features.Emissions;
using API.Shared.DataSync;
using API.Shared.DataSync.Models;
using API.Shared.EnergiDataService;
using API.Shared.EnergiDataService.Models;
using API.Shared.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    private readonly IDataSyncService dataSyncService;
    private readonly IEnergiDataService energyDataService;

    public EmissionsService(IDataSyncService dataSyncService, IEnergiDataService energyDataService)
    {
        this.dataSyncService = dataSyncService;
        this.energyDataService = energyDataService;
    }

    public async Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);
        var emissions = await energyDataService.GetEmissionsPerHour(dateFrom, dateTo);
        var measurements = await dataSyncService.GetTimeSeries(context, dateFrom, dateTo, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        return CalculateEmission(emissions, measurements, aggregation);
    }

    internal static EmissionsResponse CalculateEmission(
        IEnumerable<Shared.EnergiDataService.Models.EmissionRecord> emissions,
        IEnumerable<TimeSeries> timeSeries,
        Aggregation aggregation)
    {
        var emissionRecords = timeSeries
            .SelectMany(timeseries =>
                timeseries.Measurements.Join(
                    emissions,
                    measurement => (timeseries.MeteringPoint.GridArea, measurement.DateFrom),
                    emission => (emission.GridArea, emission.HourUTC.ToUnixTimeSeconds()),
                    (m, e) => (measurement: m, emission: e)
                )
            )
            .GroupByAggregation(it => DateTimeOffset.FromUnixTimeSeconds(it.measurement.DateFrom), aggregation)
            .Select(groupedEmission =>
            {
                var totalForBucket = groupedEmission.Sum(it => it.measurement.Quantity * it.emission.CO2PerkWh);
                var relativeForBucket = totalForBucket / groupedEmission.Sum(it => it.measurement.Quantity);
                return new Emissions.Models.EmissionRecord(
                    groupedEmission.First().measurement.DateFrom,
                    groupedEmission.Last().measurement.DateTo,
                    new Quantity(Math.Round(totalForBucket / 1000, Configurations.DecimalPrecision), QuantityUnit.g),
                    new Quantity(Math.Round(relativeForBucket, Configurations.DecimalPrecision), QuantityUnit.gPerkWh)
                );
            });

        return new EmissionsResponse(emissionRecords);
    }
}
