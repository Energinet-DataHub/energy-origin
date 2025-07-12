using System.Net;
using System.Threading.Tasks;
using API;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Tests.Extensions;
using Tests.Fixtures;
using Xunit;

namespace Tests;

public class HealthControllerTests : MeasurementsTestBase, IClassFixture<RabbitMqTesterContainer>, IClassFixture<PostgresContainer>
{
    public HealthControllerTests(TestServerFixture<Startup> serverFixture, RabbitMqTesterContainer rabbitMqTesterContainer, PostgresContainer postgresContainer)
        : base(serverFixture, rabbitMqTesterContainer.Options, postgresContainer.ConnectionString)
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
