using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api.Clients;

public interface IAuthorizationClient
{
    Task<UserOrganizationConsentsResponse?> GetConsentsAsync();
}

public class AuthorizationClient(HttpClient httpClient, IBearerTokenService bearerTokenService) : IAuthorizationClient
{

    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", bearerTokenService.GetBearerToken());
        httpClient.DefaultRequestHeaders.Add(ApiVersions.HeaderName, ApiVersions.Version1);

        return await httpClient.GetFromJsonAsync<UserOrganizationConsentsResponse>("/api/authorization/consents");
    }
}

public record UserOrganizationConsentsResponseItem(Guid ConsentId, Guid GiverOrganizationId, string GiverOrganizationTin, string GiverOrganizationName, Guid ReceiverOrganizationId, string ReceiverOrganizationTin, string ReceiverOrganizationName, long ConsentDate);
public record UserOrganizationConsentsResponse(IEnumerable<UserOrganizationConsentsResponseItem> Result);
