using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace API.MasterDataService.Clients;

public class AuthServiceClient
{
    private readonly HttpClient client;
    private readonly ILogger<AuthServiceClient> logger;
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public AuthServiceClient(HttpClient client, ILogger<AuthServiceClient> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task<string> GetUuidForCompany(string cvr)
    {
        try
        {
            var uri = $"company/uuid{new QueryBuilder { { "cvr", cvr } }}";

            var response = await client.GetAsync(uri);

            // The auth service will return 404 if the CVR is not known.
            // This situation is not explicitly handled here and it will be reported as unsuccessful
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Calling auth service was unsuccessful. Response: {response}", response);
                return string.Empty;
            }

            var content = await response.Content.ReadFromJsonAsync<CompanyUuidResponse>(jsonSerializerOptions);

            if (content == null)
            {
                logger.LogWarning("Deserializing JSON response from auth service was not successful. Content: {content}", await response.Content.ReadAsStringAsync());
                return string.Empty;
            }

            return content.Uuid;
        }
        catch (Exception e)
        {
            logger.LogWarning("Calling auth service failed. Exception: {e}", e);
            return string.Empty;
        }
    }

    private record CompanyUuidResponse(string Uuid);
}
