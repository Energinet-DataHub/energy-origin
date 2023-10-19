using System.Net;
using System.Threading.Tasks;
using API.IntegrationTests.Shared.Factories;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Shared;

[UsesVerify]
public class SwaggerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public SwaggerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        using var client = factory.CreateUnauthenticatedClient();
        using var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ContentTypeIsHtml()
    {
        using var client = factory.CreateUnauthenticatedClient();
        using var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task GetSwaggerUI_AppEnvironmentIsProduction_ReturnsNotFound()
    {
        using var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
        using var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_ReturnsOk()
    {
        using var client = factory.CreateUnauthenticatedClient();
        using var swaggerDocResponse = await client.GetAsync("api-docs/transfer/v1/swagger.json");

        swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppEnvironmentIsProduction_ReturnsOk()
    {
        using var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
        using var swaggerDocResponse = await client.GetAsync("api-docs/transfer/v1/swagger.json");

        swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        using var client = factory.CreateUnauthenticatedClient();
        using var swaggerDocResponse = await client.GetAsync("api-docs/transfer/v1/swagger.json");

        var json = await swaggerDocResponse.Content.ReadAsStringAsync();
        await Verifier.Verify(json);
    }
}
