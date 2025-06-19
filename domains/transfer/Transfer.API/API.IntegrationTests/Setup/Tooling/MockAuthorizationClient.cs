using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;

namespace API.IntegrationTests.Setup.Tooling;

public class MockAuthorizationClient : IAuthorizationClient
{
    public static List<UserOrganizationConsentsResponseItem> MockedConsents { get; set; } = [];

    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        await Task.Delay(1);
        return new UserOrganizationConsentsResponse(MockedConsents);
    }
}
