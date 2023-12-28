using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Api.v2023_01_01.Dto.Responses;
using API.IntegrationTests.Factories;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Cvr.Api.v2023_01_01.Controllers;

[UsesVerify]
public class CvrControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly WireMockServer server;

    public CvrControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        server = WireMockServer.Start();
        factory.CvrBaseUrl = server.Url!;
    }

    [Fact]
    public async Task GetCvrCompany_WhenCorrectCvrNumber_ShouldReturnCvrInfo()
    {
        var cvrNumber = "10150817";
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/v2023_01_01/Controllers/CvrControllerTests.cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
    }

    [Fact]
    public async Task GetCvrCompany_WhenWrongCvrNumber_ShouldReturnNotFound()
    {
        var cvrNumber = "123";
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/v2023_01_01/Controllers/CvrControllerTests.empty_cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetAsync($"api/cvr/{cvrNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCvrCompany_WhenTransientError_ShouldRetry()
    {
        var cvrNumber = "10150817";
        server.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .InScenario("UnstableServer")
            .WillSetStateTo("FirstCallDone")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError));

        server.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .InScenario("UnstableServer")
            .WhenStateIs("FirstCallDone")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyFromFile("Cvr/Api/v2023_01_01/Controllers/CvrControllerTests.cvr_response.json"));

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
    }
}
