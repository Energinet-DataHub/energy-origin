using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using EnergyOrigin.Domain.ValueObjects;

namespace API.IntegrationTests;

public class MockAuthorizationClient : IAuthorizationClient
{
    public static List<UserOrganizationConsentsResponseItem> MockedConsents = new();

    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        await Task.Delay(1);
        var response = new UserOrganizationConsentsResponse(MockedConsents);

        return response;
    }
}
