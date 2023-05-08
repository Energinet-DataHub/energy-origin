using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class MetricsTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public MetricsTests(TransferAgreementsApiWebApplicationFactory factory)
        => this.factory = factory;


    [Fact]
    public async Task has_metrics_endpoint()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
