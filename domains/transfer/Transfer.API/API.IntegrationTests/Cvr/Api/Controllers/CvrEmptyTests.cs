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

[Collection(IntegrationTestCollection.CollectionName)]
public class CvrEmptyTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly WireMockServer cvrWireMock;

    public CvrEmptyTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
        cvrWireMock = integrationTestFixture.CvrWireMockServer;
        cvrWireMock.ResetMappings();
    }

    [Fact]
    public async Task GetCvrCompany_WhenWrongCvrNumber_ShouldReturnEmptyOkResponse()
    {
        var cvrNumbers = new CvrRequestDto(new List<string> { "123" });
        cvrWireMock
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/Controllers/CvrControllerTests.empty_cvr_response.json")
            );

        using var client = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid());

        using var response = await client.PostAsJsonAsync("api/transfer/cvr", cvrNumbers);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content.ReadFromJsonAsync<CvrCompanyListResponse>();

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}
