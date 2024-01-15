using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests;

public class UnhealthTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public UnhealthTests(QueryApiWebApplicationFactory factory, PostgresContainer dbContainer)
    {
        this.factory = factory;
        this.factory.RabbitMqOptions = null;
        this.factory.ConnectionString = dbContainer.ConnectionString;
    }

    [Fact]
    public async Task Health_IsCalledWhenRabbitMqIsDown_ReturnsServiceUnavailable()
    {
        using var client = factory.CreateClient();
        using var healthResponse = await client.GetAsync("health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
