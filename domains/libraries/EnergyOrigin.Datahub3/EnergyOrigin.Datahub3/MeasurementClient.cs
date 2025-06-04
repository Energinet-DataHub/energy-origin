using Energinet.DataHub.Measurements.Abstractions.Api.Models;
using Energinet.DataHub.Measurements.Abstractions.Api.Queries;
using Energinet.DataHub.Measurements.Client;
using EnergyOrigin.Domain.ValueObjects;
using NodaTime;

namespace EnergyOrigin.Datahub3;

public interface IMeasurementClient
{
    Task<IEnumerable<MeasurementAggregationByPeriodDto>> GetMeasurements(IList<Gsrn> gsrn, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken);
}

public class MeasurementClient(IMeasurementsClient client) : IMeasurementClient
{
    public async Task<IEnumerable<MeasurementAggregationByPeriodDto>> GetMeasurements(IList<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken)
    {
        var query = new GetAggregateByPeriodQuery(
            [.. gsrns.Select(x => x.Value)],
            Instant.FromUnixTimeSeconds(dateFromEpoch),
            Instant.FromUnixTimeSeconds(dateToEpoch),
            Aggregation.Hour);

        return await client.GetAggregatedByPeriodAsync(query, cancellationToken);
    }
}
