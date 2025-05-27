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
        var mpIdsCsv = string.Join(",", gsrns.Select(g => g.Value));

        var url = $"/api/relation/meteringpoints/customer" +
                  $"?subject={Uri.EscapeDataString(owner)}" +
                  $"&meteringPointIds={Uri.EscapeDataString(mpIdsCsv)}";

        return await _client
            .GetFromJsonAsync<ListMeteringPointForCustomerCaResponse>(
                url,
                cancellationToken);
    }
}
