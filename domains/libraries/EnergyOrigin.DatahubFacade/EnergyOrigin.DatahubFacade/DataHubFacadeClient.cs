using System.Net.Http.Json;
using EnergyOrigin.Domain.ValueObjects;

namespace EnergyOrigin.DatahubFacade;

public interface IDataHubFacadeClient
{
    Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(
        string owner,
        List<Gsrn> gsrns,
        CancellationToken cancellationToken);
}

public class DataHubFacadeClient : IDataHubFacadeClient
{
    private readonly HttpClient _client;

    public DataHubFacadeClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(
        string owner,
        List<Gsrn> gsrns,
        CancellationToken cancellationToken)
    {
        // Join GSRNs with commas so the mock stub sees a single "meteringPointIds" param
        var mpIds = string.Join(",", gsrns.Select(x => x.Value));

        // Build the exact relative URL your stub expects:
        var url = $"api/relation/meteringpoints/customer" +
                  $"?subject={owner}" +
                  $"&meteringPointIds={mpIds}";

        return await _client
            .GetFromJsonAsync<ListMeteringPointForCustomerCaResponse>(
                url,
                cancellationToken);
    }
}
