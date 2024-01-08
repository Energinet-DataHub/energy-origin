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
//This should be in the same class as CvrControllerTests, but WireMock does not Co-operate with that.
[UsesVerify]
public class CvrMultipleResponseTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly WireMockServer server;

    public CvrMultipleResponseTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        server = WireMockServer.Start();
        factory.CvrBaseUrl = server.Url!;
    }

    [Fact]
    public async Task GetCvrCompany_CorrectAndIncorrectCvr_ShouldReturnCorrectOnly()
    {
        var wrongPlusRightCvr = new List<string> { "123", "28980671", "39315041" };
        server.ResetMappings();
        server
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/v2024_01_03/Controllers/CvrControllerTests.cvr_multiple_companies_response.json")
            );

        var client = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), apiVersion: "20240103");

        var response = await client.PostAsJsonAsync("api/cvr", wrongPlusRightCvr);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}

