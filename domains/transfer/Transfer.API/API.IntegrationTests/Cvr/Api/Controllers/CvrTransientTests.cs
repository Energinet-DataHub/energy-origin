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

public class CvrTransientTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly WireMockServer server;

    public CvrTransientTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        server = WireMockServer.Start();
        factory.CvrBaseUrl = server.Url!;
    }

    [Fact]
    public async Task GetCvrCompany_WhenTransientError_ShouldRetry()
    {
        var cvrNumbers = new CvrRequestDto(new List<string> { "10150817" });
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
                .WithBodyFromFile("Cvr/Api/Controllers/CvrControllerTests.cvr_response.json"));

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(),
            apiVersion: ApiVersions.Version20240103);

        var response = await client.PostAsJsonAsync("api/transfer/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }


}
