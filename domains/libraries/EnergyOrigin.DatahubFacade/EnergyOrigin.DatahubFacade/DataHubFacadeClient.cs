using System.Net.Http.Json;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

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
        // Build repeated "meteringPointIds" entries via StringValues
        var queryParams = new Dictionary<string, StringValues>
        {
            ["subject"] = new StringValues(owner),
            ["meteringPointIds"] = new StringValues(gsrns.Select(g => g.Value).ToArray())
        };

        var url = QueryHelpers.AddQueryString(
            "/api/relation/meteringpoints/customer",
            queryParams);

        return await _client.GetFromJsonAsync<ListMeteringPointForCustomerCaResponse>(
            url,
            cancellationToken);
    }
}
