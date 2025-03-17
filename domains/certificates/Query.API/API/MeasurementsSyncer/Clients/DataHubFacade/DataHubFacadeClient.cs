using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DataContext.ValueObjects;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer.Clients.DataHubFacade;

public interface IDataHubFacadeClient
{
    Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(string owner, List<Gsrn> gsrns, CancellationToken cancellationToken);
}

public class DataHubFacadeClient : IDataHubFacadeClient
{
    private readonly HttpClient _client;
    private readonly ILogger<DataHubFacadeClient> _logger;

    public DataHubFacadeClient(HttpClient client, ILogger<DataHubFacadeClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(string owner, List<Gsrn> gsrns, CancellationToken cancellationToken)
    {
        var mpIdsUrl = string.Join("&meteringPointIds=", gsrns.Select(x => x.Value));
        var url = $"/api/relation/meteringpoints/customer?subject={owner}&meteringPointIds={mpIdsUrl}";
        _logger.LogInformation("Url: " + url);
        return await _client.GetFromJsonAsync<ListMeteringPointForCustomerCaResponse>($"/api/relation/meteringpoints/customer?subject={owner}&meteringPointIds={mpIdsUrl}", cancellationToken);
    }
}
