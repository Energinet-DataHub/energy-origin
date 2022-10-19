using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using VerifyXunit;
using Xunit;

namespace apptests;

[UsesVerify]
public class SwaggerTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public SwaggerTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        var client = factory.CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        Assert.Equal(HttpStatusCode.OK, swaggerUiResponse.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ContentTypeIsHtml()
    {
        var client = factory.CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        Assert.Equal("text/html", swaggerUiResponse.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task GetSwaggerUI_AppEnvironmentIsProduction_ReturnsNotFound()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        Assert.Equal(HttpStatusCode.NotFound, swaggerUiResponse.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_ReturnsOk()
    {
        var client = factory.CreateClient();
        var swaggerDocResponse = await client.GetAsync("swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, swaggerDocResponse.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        var client = factory.CreateClient();
        var swaggerDocResponse = await client.GetAsync("swagger/v1/swagger.json");

        var json = await swaggerDocResponse.Content.ReadAsStringAsync();
        await Verifier.Verify(json);
    }
}
