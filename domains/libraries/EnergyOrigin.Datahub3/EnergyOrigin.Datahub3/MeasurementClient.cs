using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using Energinet.DataHub.Measurements.Abstractions.Api.Queries;
using Energinet.DataHub.Measurements.Client;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace EnergyOrigin.Datahub3;

public interface IMeasurementClient
{
    Task<IEnumerable<MeasurementAggregationByPeriodDto>> GetMeasurements(IList<Gsrn> gsrn, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken);
}

public class MeasurementClient(IMeasurementsClient client, IOptions<DataHub3Options> options, ILogger<MeasurementClient> logger) : IMeasurementClient
{
    public async Task<IEnumerable<MeasurementAggregationByPeriodDto>> GetMeasurements(
            IList<Gsrn> gsrns,
            long dateFromEpoch,
            long dateToEpoch,
            CancellationToken cancellationToken)
    {
        if (options.Value.EnableMock)
        {
            return await GetAggregatedByPeriodForMockData(gsrns, dateFromEpoch, dateToEpoch, cancellationToken);
        }

        var query = new GetAggregateByPeriodQuery(
            [.. gsrns.Select(x => x.Value)],
            Instant.FromUnixTimeSeconds(dateFromEpoch),
            Instant.FromUnixTimeSeconds(dateToEpoch),
            Aggregation.Hour);

        return await client.GetAggregatedByPeriodAsync(query, cancellationToken);
    }

    private async Task<IEnumerable<MeasurementAggregationByPeriodDto>> GetAggregatedByPeriodForMockData(IList<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<IEnumerable<MeasurementAggregationByPeriodDto>>>();
        foreach (var gsrn in gsrns)
        {
            var singleGsrnQuery = new GetAggregateByPeriodQuery(
                [gsrn.Value],
                Instant.FromUnixTimeSeconds(dateFromEpoch),
                Instant.FromUnixTimeSeconds(dateToEpoch),
                Aggregation.Hour);

            tasks.Add(client.GetAggregatedByPeriodAsync(singleGsrnQuery, cancellationToken));
        }


        var responses = await Task.WhenAll(tasks);

        List<MeasurementAggregationByPeriodDto> combinedAggregations = [];
        foreach (var aggregations in responses)
        {
            if (aggregations.Any())
            {
                combinedAggregations.Add(aggregations.First());
            }
        }

        return combinedAggregations;
    }
}
