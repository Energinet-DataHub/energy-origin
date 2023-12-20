using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Api;
using Xunit;

namespace Worker.IntegrationTest.Api;

public class TransferAutomationControllerTest : IClassFixture<TransferAgreementApplicationFactory>
{
    private readonly HttpClient authenticatedClient;
    private readonly AutomationCache cache;

    public TransferAutomationControllerTest(TransferAgreementApplicationFactory factory)
    {
        authenticatedClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());
        cache = factory.Services.GetService<AutomationCache>()!;
    }

    [Fact]
    public async Task can_get_healthy_status()
    {
        cache.Cache.Set(HealthEntries.Key, HealthEntries.Healthy, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
        });

        var response = await authenticatedClient.GetFromJsonAsync<TransferAutomationStatus>("/api/transfer-automation/status");

        response.Should().BeEquivalentTo(new TransferAutomationStatus(HealthEntries.Healthy));
    }
}
