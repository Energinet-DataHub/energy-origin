using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Dto.Requests;
using API.Cvr.Api.Dto.Responses;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Controllers;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Cvr.Api.Controllers;

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
        var cvrNumbers = new CvrRequestDto(new List<string> { "10150817" });
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/Controllers/CvrControllerTests.cvr_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(),
            apiVersion: ApiVersions.Version20240103);

        var response = await client.PostAsJsonAsync("api/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}
