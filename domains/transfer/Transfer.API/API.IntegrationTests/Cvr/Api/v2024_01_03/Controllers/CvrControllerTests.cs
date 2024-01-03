using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Api.v2024_01_03.Dto.Responses;
using API.IntegrationTests.Factories;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Cvr.Api.v2024_01_03.Controllers;

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
    public async Task GetCvrCompanies_WhenCorrectCvrNumbers_ShouldReturnCvrInfo()
    {
        var cvrNumbers = new List<string> { "10150817" };
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/v2024_01_03/Controllers/CvrControllerTests.cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), apiVersion: "20240103");

        var response = await client.PostAsJsonAsync("api/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }


    [Fact]
    public async Task GetCvrCompany_WhenWrongCvrNumber_ShouldReturnEmptyOkResponse()
    {
        var cvrNumbers = new List<string> { "123" };
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/v2024_01_03/Controllers/CvrControllerTests.empty_cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), apiVersion: "20240103");

        var response = await client.PostAsJsonAsync("api/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }

    // [Fact]
    // public async Task GetCvrCompany_WhenWrongPlusRight_ShouldReturnRightOnly()
    // {
    //     var wrongPlusRightCvr = new List<string> { "123", "10150817" };
    //     server.ResetMappings();
    //     server
    //         .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
    //         .RespondWith(Response.Create()
    //             .WithStatusCode(200)
    //             .WithBodyFromFile("Cvr/Api/v2024_01_03/Controllers/CvrControllerTests.empty_cvr_response.json")
    //         );
    //
    //     var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), apiVersion: "20240103");
    //
    //     var response = await client.PostAsJsonAsync("api/cvr", wrongPlusRightCvr);
    //
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    //
    //     var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();
    //
    //     var settings = new VerifySettings();
    //     settings.DontScrubGuids();
    //     await Verifier.Verify(jsonContent, settings);
    // }


    [Fact]
    public async Task GetCvrCompany_WhenTransientError_ShouldRetry()
    {
        var cvrNumbers = new List<string> { "10150817" };
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
                .WithBodyFromFile("Cvr/Api/v2024_01_03/Controllers/CvrControllerTests.cvr_response.json"));

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), apiVersion: "20240103");

        var response = await client.PostAsJsonAsync("api/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}
