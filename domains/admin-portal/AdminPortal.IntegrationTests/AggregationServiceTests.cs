using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPortal.IntegrationTests;

public class AggregationServiceTests
{
    [Fact]
    public async Task GetActiveContractsAsync_ReturnsSpecifiedNumberOfEntries()
    {
        var mockHandler = new MockHttpMessageHandler { MockEntryCount = 50 };
        await using var factory = new CustomWebApplicationFactory(mockHandler);

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        var result = await service.GetActiveContractsAsync();

        Assert.Equal(50, result.Results.MeteringPoints.Count);
    }

    [Fact]
    public async Task GetActiveContractsAsync_HandlesApiFailure()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetFirstPartyApiResponse(HttpStatusCode.InternalServerError);

        await using var factory = new CustomWebApplicationFactory(mockHandler);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAggregationService>();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetActiveContractsAsync());
    }
}
