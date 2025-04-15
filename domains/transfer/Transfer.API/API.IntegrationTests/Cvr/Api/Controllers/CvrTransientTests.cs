using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Dto.Requests;
using API.Cvr.Api.Dto.Responses;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using API.Transfer.Api.Controllers;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Cvr.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class CvrTransientTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly WireMockServer cvrWireMock;

    public CvrTransientTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
        cvrWireMock = integrationTestFixture.CvrWireMockServer;
        cvrWireMock.ResetMappings();
    }

    [Fact]
    public async Task GetCvrCompany_WhenTransientError_ShouldRetry()
    {
        var cvrNumbers = new CvrRequestDto(new List<string> { "10150817" });
        cvrWireMock.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .InScenario("UnstableServer")
            .WillSetStateTo("FirstCallDone")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError));

        cvrWireMock.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .InScenario("UnstableServer")
            .WhenStateIs("FirstCallDone")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyFromFile("Cvr/Api/Controllers/CvrControllerTests.cvr_response.json"));

        using var client = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid());

        using var response = await client.PostAsJsonAsync("api/transfer/cvr", cvrNumbers, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>(TestContext.Current.CancellationToken);

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}
