using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Cvr.Api.Dto.Responses.Internal;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using FluentAssertions;
using VerifyTests;
using VerifyXunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Cvr.Api.Controllers.Internal;

[Collection(IntegrationTestCollection.CollectionName)]
public class InternalCvrControllerTests
{
    private readonly TransferAgreementsApiWebApplicationFactory _factory;
    private readonly WireMockServer _cvrWireMock;

    public InternalCvrControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _factory = integrationTestFixture.Factory;
        _cvrWireMock = integrationTestFixture.CvrWireMockServer;
        _cvrWireMock.ResetMappings();
    }

    [Fact]
    public async Task GetCvrCompaniesInternal_WhenCorrectCvrNumbers_ShouldReturnCvrInfo()
    {
        var cvr = "10150817";
        _cvrWireMock
            .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyFromFile("Cvr/Api/Controllers/CvrControllerTests.cvr_response.json")
            );

        using var client = _factory.CreateB2CAuthenticatedClient(
            Guid.Parse("8bb12660-aa0e-4eef-a4aa-d6cd62615201"),
            Guid.NewGuid());

        using var response = await client
            .GetAsync($"api/transfer/admin-portal/internal-cvr/companies/{cvr}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await response.Content
            .ReadFromJsonAsync<CvrCompanyInformationDto>(TestContext.Current.CancellationToken);

        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(jsonContent, settings);
    }
}
