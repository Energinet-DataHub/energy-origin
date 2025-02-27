using System.Net;
using System.Threading.Tasks;
using API;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Tests.Extensions;
using Tests.Fixtures;
using Xunit;

namespace Tests;

public class HealthControllerTests : MeasurementsTestBase, IClassFixture<RabbitMqContainer>, IClassFixture<PostgresContainer>
{
    public HealthControllerTests(TestServerFixture<Startup> serverFixture, RabbitMqContainer rabbitMqContainer, PostgresContainer postgresContainer)
        : base(serverFixture, rabbitMqContainer.Options, postgresContainer.ConnectionString)
    {
    }

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var client = _serverFixture.CreateUnauthenticatedClient();
        var response = await client.RepeatedlyQueryUntil("/health", response => response.IsSuccessStatusCode);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
