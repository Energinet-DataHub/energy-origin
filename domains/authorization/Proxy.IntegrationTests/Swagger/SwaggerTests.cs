using System.Net;
using FluentAssertions;
using Proxy.IntegrationTests.Setup;
using Proxy.IntegrationTests.Swagger;

namespace Proxy.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class SwaggerTests(ProxyIntegrationTestFixture proxyIntegrationTestFixture)
{
    private readonly SwaggerWebApplicationFactory factory = proxyIntegrationTestFixture.SwaggerFactory;

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        var client = factory.CreateClient();
        using var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ContentTypeIsHtml()
    {
        using var client = factory.CreateClient();
        using var swaggerUiResponse = await client.GetAsync("swagger");

        swaggerUiResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task GetSwaggerDocs_AllVersions_ReturnsOk()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();
            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                using var swaggerDocResponse = await client.GetAsync($"api-docs/authorization-proxy/{version}/swagger.json");
                swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                    $"Swagger documentation for version {version} should be accessible.");
            }
        });
    }

    [Fact]
    public async Task SwaggerDocs_AllVersions_MatchSnapshots()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();
            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                var swaggerDocUrl = $"api-docs/authorization-proxy/{version}/swagger.json";
                using var response = await client.GetAsync(swaggerDocUrl);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                await Verify(json)
                    .UseParameters(version)
                    .UseMethodName($"GetSwaggerDocs_v{version}");
            }
        });
    }
}
