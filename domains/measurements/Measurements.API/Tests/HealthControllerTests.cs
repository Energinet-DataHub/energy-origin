using System.Net;
using System.Threading.Tasks;
using API;
using Tests.Fixtures;
using Xunit;

namespace Tests;

public class HealthControllerTests : MeasurementsTestBase
{
    public HealthControllerTests(TestServerFixture<Startup> serverFixture)
        : base(serverFixture)
    {
    }

    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var response = await _serverFixture.CreateUnauthenticatedClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
