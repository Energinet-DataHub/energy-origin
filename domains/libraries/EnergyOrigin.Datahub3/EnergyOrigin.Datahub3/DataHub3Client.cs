using System.Net.Http.Json;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.Datahub3;

public interface IDataHub3Client
{
    Task<MeteringPointData[]?> GetMeasurements(List<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken);
}

public class DataHub3Client : IDataHub3Client
{
    private readonly HttpClient _client;
    private readonly bool _useMock;

    public DataHub3Client(
        HttpClient client,
        IOptions<DataHub3Options> opts)
    {
        _client = client;
        _useMock = opts.Value.EnableMock;
    }

    public async Task<MeteringPointData[]?> GetMeasurements(
        List<Gsrn> gsrns,
        long dateFromEpoch,
        long dateToEpoch,
        CancellationToken cancellationToken)
    {
        if (_useMock)
        {
            var allResults = new List<MeteringPointData>();
            foreach (var g in gsrns)
            {
                var url = $"/ListAggregatedTimeSeries"
                          + $"?meteringPointIds={g.Value}"
                          + $"&dateFromEpoch={dateFromEpoch}"
                          + $"&dateToEpoch={dateToEpoch}"
                          + $"&Aggregation=Hour";

                var single = await _client
                    .GetFromJsonAsync<MeteringPointData[]?>(
                        url,
                        cancellationToken: cancellationToken);

                if (single != null)
                    allResults.AddRange(single);
            }

            return allResults.ToArray();
        }
        else
        {
            var meteringPointIds = string.Join(",", gsrns.Select(x => x.Value));
            var url = $"/ListAggregatedTimeSeries"
                      + $"?meteringPointIds={meteringPointIds}"
                      + $"&dateFromEpoch={dateFromEpoch}"
                      + $"&dateToEpoch={dateToEpoch}"
                      + $"&Aggregation=Hour";

            return await _client
                .GetFromJsonAsync<MeteringPointData[]?>(
                    url,
                    cancellationToken: cancellationToken);
        }
    }
}
