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

[Collection(IntegrationTestCollection.CollectionName)]
public class SwaggerTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public SwaggerTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
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
    public async Task GetSwaggerUI_Production_ReturnsNotFound()
    {
        using var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var swaggerUiResponse = await client.GetAsync("swagger");
        swaggerUiResponse.StatusCode.Should()
            .Be(HttpStatusCode.NotFound, "Swagger UI should not be accessible in production.");
    }

    [Fact]
    public async Task GetSwaggerDocs_AllVersions_ReturnsOk()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateUnauthenticatedClient();
            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                using var swaggerDocResponse = await client.GetAsync($"api-docs/transfer/{version}/swagger.json");
                swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                    $"Swagger documentation for version {version} should be accessible.");
            }
        });
    }

    [Fact]
    public async Task GetSwaggerDocs_ForAllVersions_Production_ReturnsOk()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
                .CreateClient();

            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                using var swaggerDocResponse = await client.GetAsync($"api-docs/transfer/{version}/swagger.json");
                swaggerDocResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                    $"Swagger documentation for version {version} should be accessible in production.");
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
                var swaggerDocUrl = $"api-docs/transfer/{version}/swagger.json";
                using var response = await client.GetAsync(swaggerDocUrl);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                await Verifier.Verify(json)
                    .UseParameters(version)
                    .UseMethodName($"GetSwaggerDocs_v{version}");
            }
        });
    }

    [Fact]
    public async Task SwaggerDoc_ContainsBearerSecurityDefinition()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();

            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                var swaggerDocUrl = $"api-docs/transfer/{version}/swagger.json";
                using var response = await client.GetAsync(swaggerDocUrl);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var components = doc.RootElement.GetProperty("components");
                var securitySchemes = components.GetProperty("securitySchemes");

                securitySchemes.TryGetProperty("Bearer", out _).Should().BeTrue(
                    $"Swagger JSON for API version {version} should contain a Bearer security scheme.");
            }
        });
    }

    [Fact]
    public async Task SwaggerDoc_VerifiesFullBearerSecuritySchemeStructure()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();

            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                var swaggerDocUrl = $"api-docs/transfer/{version}/swagger.json";
                using var response = await client.GetAsync(swaggerDocUrl);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var components = doc.RootElement.GetProperty("components");
                var securitySchemes = components.GetProperty("securitySchemes");

                var securitySchemesJson = securitySchemes.GetRawText();

                await Verifier.Verify(securitySchemesJson)
                    .UseParameters(version)
                    .UseMethodName($"SwaggerDoc_{version}_VerifiesFullBearerSecuritySchemeStructure");
            }
        });
    }

    [Fact]
    public async Task SwaggerDoc_ContainsSecurityRequirementForBearer()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();

            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                var swaggerDocUrl = $"api-docs/transfer/{version}/swagger.json";
                using var response = await client.GetAsync(swaggerDocUrl);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var securityRequirements = doc.RootElement.GetProperty("security").EnumerateArray().ToList();

                var containsBearer = securityRequirements.Any(securityRequirement =>
                    securityRequirement.EnumerateObject().Any(securityScheme =>
                        securityScheme.Name.Equals("Bearer")));

                containsBearer.Should().BeTrue(
                    $"Swagger JSON for API version {version} should contain a security requirement for Bearer.");
            }
        });
    }

    [Fact]
    public async Task SwaggerDoc_VerifiesFullSecurityRequirementsStructure()
    {
        await factory.WithApiVersionDescriptionProvider(async apiDesc =>
        {
            using var client = factory.CreateClient();

            foreach (var version in apiDesc.ApiVersionDescriptions.Select(v => v.GroupName))
            {
                var swaggerJsonUrl = $"api-docs/transfer/{version}/swagger.json";
                using var swaggerResponse = await client.GetAsync(swaggerJsonUrl);
                swaggerResponse.EnsureSuccessStatusCode();
                var swaggerJson = await swaggerResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(swaggerJson);
                var security = doc.RootElement.TryGetProperty("security", out var securityElement) ? securityElement.GetRawText() : null;

                await Verifier.Verify(security)
                    .UseParameters(version)
                    .UseMethodName($"SwaggerDoc_{version}_VerifiesFullSecurityRequirementsStructure");
            }
        });
    }
}
