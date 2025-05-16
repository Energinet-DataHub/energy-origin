using System.Net.Http.Json;
using EnergyOrigin.Domain.ValueObjects;

namespace EnergyOrigin.DatahubFacade;

public interface IDataHubFacadeClient
{
    Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(string owner, List<Gsrn> gsrns, CancellationToken cancellationToken);
}

public class DataHubFacadeClient : IDataHubFacadeClient
{
    private readonly HttpClient _client;

    public DataHubFacadeClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ListMeteringPointForCustomerCaResponse?> ListCustomerRelations(string owner, List<Gsrn> gsrns, CancellationToken cancellationToken)
    {
        var mpIdsUrl = string.Join("&meteringPointIds=", gsrns.Select(x => x.Value));
        return await _client.GetFromJsonAsync<ListMeteringPointForCustomerCaResponse>($"/api/relation/meteringpoints/customer?subject={owner}&meteringPointIds={mpIdsUrl}", cancellationToken);
    }
}
