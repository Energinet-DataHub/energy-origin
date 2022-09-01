using API.Configuration;
using API.EnergySources.Models;
using API.Extensions;
using API.Features.EnergySources;
using API.Shared.DataSync;
using API.Shared.DataSync.Models;
using API.Shared.EnergiDataService;
using API.Shared.EnergiDataService.Models;
using API.Shared.Models;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;

namespace API.Services;

public class SourcesService : ISourcesService
{
    private static readonly IList<string> renewableSources = Configurations.GetRenewableSources();
    private static readonly decimal wasteRenewableShare = Configurations.GetWasteRenewableShare();
    private const string waste = "waste";


    private readonly IDataSyncService dataSyncService;
    private readonly IEnergiDataService energyDataService;

    public SourcesService(IDataSyncService dataSyncService, IEnergiDataService energyDataService)
    {
        this.dataSyncService = dataSyncService;
        this.energyDataService = energyDataService;
    }

    public async Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);
        var measurements = await dataSyncService.GetTimeSeries(context, dateFrom, dateTo, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));
        var mixRecords = await energyDataService.GetResidualMixPerHour(dateFrom, dateTo);

        return CalculateSourceEmissions(measurements, mixRecords, aggregation);
    }

    internal static EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> timeSeries, IEnumerable<MixRecord> records, Aggregation aggregation)
    {
        var emissionRecords = timeSeries
            .SelectMany(timeseries =>
                timeseries.Measurements.Join(
                    records.GroupBy(it => (it.GridArea, it.HourUTC)),
                    measurement => (timeseries.MeteringPoint.GridArea, measurement.DateFrom),
                    emission => (emission.Key.GridArea, emission.Key.HourUTC.ToUnixTime()),
                    (m, r) => (measurement: m, record: r)
                )
            )
            .GroupByAggregation(it => DateTimeOffset.FromUnixTimeSeconds(it.measurement.DateFrom), aggregation)
            .Select(group =>
            {
                var totalQuantityForGroup = group.Sum(it => it.measurement.Quantity);

                var sourcesWithPercentage = group
                    .SelectMany(list => list.record.Select(it => (it.ProductionType, Amount: it.ShareTotal * list.measurement.Quantity)))
                    .GroupBy(it => it.ProductionType)
                    .ToDictionary(group => group.Key, group => Math.Round(group.Sum(tuple => tuple.Amount) / totalQuantityForGroup / 100, Configurations.DecimalPrecision));

                var renewablePercentage = sourcesWithPercentage
                    .Where(a => renewableSources.Contains(a.Key))
                    .Sum(a => a.Value * (a.Key == waste ? wasteRenewableShare : 1));

                return new EnergySourceDeclaration(
                    group.First().measurement.DateFrom,
                    group.Last().measurement.DateTo,
                    renewablePercentage,
                    sourcesWithPercentage
                );
            });

        return new EnergySourceResponse(emissionRecords);
    }
}
