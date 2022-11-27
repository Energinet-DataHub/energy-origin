using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.MasterDataService.AuthService;

public class AuthServiceClient
{
    private readonly HttpClient client;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public AuthServiceClient(HttpClient client)
    {
        this.client = client;
        jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
        };
    }

    public async Task<string> GetUuid(string cvr)
    {
        var queryBuilder = new QueryBuilder { { "cvr", cvr } };
        var uriBuilder = new UriBuilder("company/uuid")
        {
            Query = queryBuilder.ToString()
        };

        var response = await client.GetFromJsonAsync<CompanyUuidResponse>(uriBuilder.Uri, jsonSerializerOptions);
        return response?.Uuid ?? "todo"; //TODO: Do better here
    }

    private record CompanyUuidResponse(string Uuid);
}
