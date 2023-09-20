using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.IntegrationTests.Factories;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Controllers;

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
                .WithBodyFromFile("Controllers/CvrControllerTests.cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();
        response.CompanyCvr.Should().Be(cvrNumber);
        response.Address.Should().NotBeNull();
        response.Address.Kommune.Should().NotBeNull();
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
                .WithBodyFromFile("Controllers/CvrControllerTests.empty_cvr_response.json")
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
                .WithBodyFromFile("Controllers/CvrControllerTests.cvr_response.json"));

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<CvrCompanyDto>($"api/cvr/{cvrNumber}");

        response.Should().NotBeNull();
        response.CompanyCvr.Should().Be(cvrNumber);
    }
}
