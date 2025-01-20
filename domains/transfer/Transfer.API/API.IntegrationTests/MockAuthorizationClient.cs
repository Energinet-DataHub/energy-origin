using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using Microsoft.Extensions.Options;

namespace API.IntegrationTests;

public class MockAuthorizationClient(IOptionsMonitor<TestContext> testContext) : IAuthorizationClient
{
    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        await Task.Delay(1);

        var consentList = testContext.CurrentValue.ConsentList;

        return consentList != null
            ? new UserOrganizationConsentsResponse(consentList)
            : new UserOrganizationConsentsResponse(new List<UserOrganizationConsentsResponseItem>());
    }
}

public class TestContext
{
    public List<UserOrganizationConsentsResponseItem>? ConsentList { get; set; }
}

public class TestContextMonitor : IOptionsMonitor<TestContext>
{
    private TestContext _currentValue = new();
    public TestContext CurrentValue => _currentValue;

    public TestContext Get(string? name) => _currentValue;
    public IDisposable OnChange(Action<TestContext, string> listener) => throw new NotImplementedException();
    public void Set(List<UserOrganizationConsentsResponseItem> consentList) => _currentValue = new TestContext { ConsentList = consentList };
}

