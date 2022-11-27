using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace API.MasterDataService.AuthService;

public class AuthServiceClient
{
    private readonly HttpClient client;
    private readonly ILogger<AuthServiceClient> logger;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public AuthServiceClient(HttpClient client, ILogger<AuthServiceClient> logger)
    {
        this.client = client;
        this.logger = logger;

        jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
        };
    }

    public async Task<string> GetUuid(string cvr)
    {
        var queryBuilder = new QueryBuilder { { "cvr", cvr } };
        var uri = $"company/uuid{queryBuilder}";

        logger.LogInformation("Requesting {uri}", uri);
        var response = await client.GetFromJsonAsync<CompanyUuidResponse>(uri, jsonSerializerOptions);
        logger.LogInformation("Response: {response}", response);

        return response?.Uuid ?? "todo"; //TODO: Do better here
    }

    private record CompanyUuidResponse(string Uuid);
}
