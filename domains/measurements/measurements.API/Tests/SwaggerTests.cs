using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using Tests.Fixtures;
using VerifyXunit;
using Xunit;

namespace Tests;

[UsesVerify]
public class SwaggerTests : MeasurementsTestBase
{
    public SwaggerTests(TestServerFixture<Startup> serverFixture)
        : base(serverFixture)
    {
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client = _serverFixture.CreateUnauthenticatedClient();
        using var swaggerUiResponse = await client.GetAsync("swagger/index.html");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ContentTypeIsHtml()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client = _serverFixture.CreateUnauthenticatedClient();
        using var swaggerUiResponse = await client.GetAsync("swagger/index.html");

        swaggerUiResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task GetSwaggerUI_Production_ReturnsNotFound()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client =
            _serverFixture.CreateUnauthenticatedClient(environment: "Production");

        var swaggerUiResponse = await client.GetAsync("swagger/index.html");
        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "Swagger UI should not be accessible in production.");
    }

    [Fact]
    public async Task GetSwaggerDocs_AllVersions_ReturnsOk()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client =
            _serverFixture.CreateUnauthenticatedClient();

        var provider = _serverFixture.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerDocResponse = await client.GetAsync($"api-docs/measurements/{version}/swagger.json");
            swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Swagger documentation for version {version} should be accessible.");
        }
    }

    [Fact]
    public async Task GetSwaggerDocs_ForAllVersions_Production_ReturnsOk()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client =
            _serverFixture.CreateUnauthenticatedClient(environment: "Production");

        var provider = _serverFixture.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerDocResponse = await client.GetAsync($"api-docs/measurements/{version}/swagger.json");
            swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Swagger documentation for version {version} should be accessible in production.");
        }
    }

    [Fact]
    public async Task SwaggerDocs_AllVersions_MatchSnapshots()
    {
        _serverFixture.RefreshHostOnNextClient();
        using var client =
            _serverFixture.CreateUnauthenticatedClient();
        var provider = _serverFixture.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerDocUrl = $"api-docs/measurements/{version}/swagger.json";
            var response = await client.GetAsync(swaggerDocUrl);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            await Verifier.Verify(json)
                .UseParameters(version)
                .UseMethodName($"GetSwaggerDocs_v{version}");
        }
    }
}
