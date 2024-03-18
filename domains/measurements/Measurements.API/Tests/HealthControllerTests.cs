using System.Net;
using System.Threading.Tasks;
using API;
using Tests.Fixtures;
using Tests.TestContainers;
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
        var response = await _serverFixture.CreateUnauthenticatedClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
