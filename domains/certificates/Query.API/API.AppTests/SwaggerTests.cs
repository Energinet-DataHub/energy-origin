using System.Net;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using VerifyXunit;
using Xunit;

namespace API.AppTests;

[UsesVerify]
public class SwaggerTests : IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public SwaggerTests(QueryApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        var client = factory.CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ContentTypeIsHtml()
    {
        var client = factory.CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task GetSwaggerUI_AppEnvironmentIsProduction_ReturnsNotFound()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
        var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_ReturnsOk()
    {
        var client = factory.CreateClient();
        var swaggerDocResponse = await client.GetAsync("api-docs/certificates/v1/swagger.json");

        swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppEnvironmentIsProduction_ReturnsOk()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
        var swaggerDocResponse = await client.GetAsync("api-docs/certificates/v1/swagger.json");

        swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        var client = factory.CreateClient();
        var swaggerDocResponse = await client.GetAsync("api-docs/certificates/v1/swagger.json");

        var json = await swaggerDocResponse.Content.ReadAsStringAsync();
        await Verifier.Verify(json);
    }
}
