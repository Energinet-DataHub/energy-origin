using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api.Clients;

public interface IAuthorizationClient
{
    Task<UserOrganizationConsentsResponse?> GetConsentsAsync();
}

public class AuthorizationClient(HttpClient httpClient, IBearerTokenService bearerTokenService, ILogger<AuthorizationClient> logger) : IAuthorizationClient
{
    private readonly ILogger<AuthorizationClient> _logger = logger;

    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        _logger.LogInformation("Trying to fetch consents from authorization.");
        httpClient.DefaultRequestHeaders.Add("Authorization", bearerTokenService.GetBearerToken());
        httpClient.DefaultRequestHeaders.Add("X-API-Version", "1");

        var response = await httpClient.GetAsync("/api/authorization/consents");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch consents from authorization. Status) {StatusCode}", response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<UserOrganizationConsentsResponse>();
    }
}


public record UserOrganizationConsentsResponseItem(Guid ConsentId, Guid GiverOrganizationId, string GiverOrganizationTin, string GiverOrganizationName, Guid ReceiverOrganizationId, string ReceiverOrganizationTin, string ReceiverOrganizationName, long ConsentDate);
public record UserOrganizationConsentsResponse(IEnumerable<UserOrganizationConsentsResponseItem> Result);
