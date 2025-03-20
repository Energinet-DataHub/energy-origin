using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DataContext.ValueObjects;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer.Clients.DataHub3;

public interface IDataHub3Client
{
    Task<MeteringPointData[]?> GetMeasurements(List<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken);
}

public class DataHub3Client : IDataHub3Client
{
    private readonly HttpClient _client;

    public DataHub3Client(HttpClient client)
    {
        _client = client;
    }

    public async Task<MeteringPointData[]?> GetMeasurements(List<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken)
    {
        var meteringPointIds = string.Join(",", gsrns.Select(x => x.Value));
        var url = $"/ListAggregatedTimeSeries?meteringPointIds={meteringPointIds}&dateFromEpoch={dateFromEpoch}&dateToEpoch={dateToEpoch}&Aggregation=Hour";
        return await _client.GetFromJsonAsync<MeteringPointData[]>(url, cancellationToken: cancellationToken);
    }
}
