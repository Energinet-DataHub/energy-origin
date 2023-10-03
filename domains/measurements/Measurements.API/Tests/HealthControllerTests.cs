using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Integration.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public async Task Health_ShouldReturnOk_WhenStarted()
    {
        var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
