using System.Net;
using System.Threading.Tasks;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminPortal.Tests.Controllers;

public class MeteringpointsControllerTest
{
    [Fact]
    public async Task Index_ReturnsOk()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var response = await client.GetAsync("Meteringpoints?Tin=12345678", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
