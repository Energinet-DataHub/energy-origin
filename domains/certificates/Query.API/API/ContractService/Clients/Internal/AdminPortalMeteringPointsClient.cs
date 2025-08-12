using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Models;
using Microsoft.AspNetCore.Http;

namespace API.ContractService.Clients.Internal;

public class AdminPortalMeteringPointsClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IAdminPortalMeteringPointsClient
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public async Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();

        var meteringPointsUrl = $"/api/measurements/admin-portal/internal-meteringpoints?organizationId={owner}";

        return await httpClient.GetFromJsonAsync<MeteringPointsResponse>(meteringPointsUrl, jsonSerializerOptions, cancellationToken);
    }

    private void ValidateHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new HttpRequestException($"No HTTP context found. {nameof(AdminPortalMeteringPointsClient)} must be used as part of a request");
        }
    }

    private void SetAuthorizationHeader()
    {
        httpClient.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext!.Request.Headers.Authorization!);
    }
}
