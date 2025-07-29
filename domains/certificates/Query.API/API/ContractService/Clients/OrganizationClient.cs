using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace API.ContractService.Clients;

public class OrganizationClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IOrganizationClient
{
    private readonly HttpClient httpClient = httpClient;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AdminPortalOrganizationResponse?> GetOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        ValidateHttpContext();
        SetAuthorizationHeader();

        var meteringPointsUrl = $"api/authorization/admin-portal/organizations/{organizationId}";

        return await httpClient.GetFromJsonAsync<AdminPortalOrganizationResponse>(meteringPointsUrl, jsonSerializerOptions, cancellationToken);
    }

    private void SetAuthorizationHeader()
    {
        httpClient.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext!.Request.Headers.Authorization!);
    }

    private void ValidateHttpContext()
    {
        if (httpContextAccessor.HttpContext is null)
        {
            throw new HttpRequestException($"No HTTP context found. {nameof(OrganizationClient)} must be used as part of a request");
        }
    }
}

public record AdminPortalOrganizationResponse(Guid OrganizationId, string OrganizationName, string? Tin, OrganizationStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrganizationStatus
{
    [EnumMember(Value = "Trial")]
    Trial,

    [EnumMember(Value = "Normal")]
    Normal,

    [EnumMember(Value = "Deactivated")]
    Deactivated
}
