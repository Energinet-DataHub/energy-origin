using System;
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
    private readonly ILogger<DataHub3Client> _logger;

    public DataHub3Client(HttpClient client, ILogger<DataHub3Client> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<MeteringPointData[]?> GetMeasurements(List<Gsrn> gsrns, long dateFromEpoch, long dateToEpoch, CancellationToken cancellationToken)
    {
        var meteringPointIds = string.Join(",", gsrns.Select(x => x.Value));
        var url = $"/ListAggregatedTimeSeries?meteringPointIds={meteringPointIds}&dateFromEpoch={dateFromEpoch}&dateToEpoch={dateToEpoch}&Aggregation=Hour";

        var baseUrl = _client.BaseAddress;
        _logger.LogError("Url: " + baseUrl + url);

        return await _client.GetFromJsonAsync<MeteringPointData[]>(url, cancellationToken: cancellationToken);
    }
}
