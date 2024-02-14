using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests;

public class SwaggerTests : TestBase, IClassFixture<QueryApiWebApplicationFactory>
{
    private readonly QueryApiWebApplicationFactory factory;

    public SwaggerTests(QueryApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSwaggerUI_AppStarted_ReturnsOk()
    {
        using var client = factory.CreateClient();
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
        using var client = factory.CreateClient();
        var provider = factory.GetApiVersionDescriptionProvider();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            using var swaggerDocResponse = await client.GetAsync($"api-docs/certificates/{version}/swagger.json");
            swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetSwaggerDoc_AppEnvironmentIsProduction_ReturnsOk()
    {
        using var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var provider = factory.GetApiVersionDescriptionProvider();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            using var swaggerDocResponse = await client.GetAsync($"api-docs/certificates/{version}/swagger.json");
            swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        using var client = factory.CreateClient();

        var provider = factory.GetApiVersionDescriptionProvider();

        foreach (var version in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerDocUrl = $"api-docs/certificates/{version}/swagger.json";
            var response = await client.GetAsync(swaggerDocUrl);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            await Verifier.Verify(json)
                .UseParameters(version)
                .UseMethodName($"GetSwaggerDocs_v{version}");
        }
    }

    [Fact]
    public async Task SwaggerJson_ForAllVersions_ContainsContractsTag()
    {
        using var client = factory.CreateClient();
        var provider = factory.GetApiVersionDescriptionProvider();

        foreach (var description in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerJsonUrl = $"api-docs/certificates/{description}/swagger.json";
            var swaggerResponse = await client.GetAsync(swaggerJsonUrl);
            swaggerResponse.EnsureSuccessStatusCode();
            var swaggerJson = await swaggerResponse.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(swaggerJson);
            var tags = doc.RootElement.GetProperty("tags");

            var containsContractsTag = tags.EnumerateArray().Any(tag => tag.TryGetProperty("name", out var name) && name.GetString() == "Contracts");

            containsContractsTag.Should().BeTrue($"API version {description} should contain a 'Contracts' tag.");
        }
    }

    [Fact]
    public async Task SwaggerJson_ForAllVersions_TagHasCorrectContent()
    {
        using var client = factory.CreateClient();
        var provider = factory.GetApiVersionDescriptionProvider();

        foreach (var description in provider.ApiVersionDescriptions.Select(v => v.GroupName))
        {
            var swaggerJsonUrl = $"api-docs/certificates/{description}/swagger.json";
            var swaggerResponse = await client.GetAsync(swaggerJsonUrl);
            swaggerResponse.EnsureSuccessStatusCode();
            var swaggerJson = await swaggerResponse.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(swaggerJson);
            var tags = doc.RootElement.GetProperty("tags");

            var contractsTag = tags.EnumerateArray()
                .FirstOrDefault(tag => tag.GetProperty("name").GetString() == "Contracts");

            var tagDetails = new
            {
                Name = contractsTag.GetProperty("name").GetString(),
                Description = contractsTag.GetProperty("description").GetString()
            };

            await Verifier.Verify(tagDetails)
                .UseParameters(description)
                .UseMethodName($"SwaggerJson_{description}_VerifyContractsTagContent");
        }
    }
}
