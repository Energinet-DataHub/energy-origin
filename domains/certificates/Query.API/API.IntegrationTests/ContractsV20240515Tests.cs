using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Controllers;
using FluentAssertions;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class ContractsV20240515Tests
{

    private readonly QueryApiWebApplicationFactory factory;
    private readonly MeasurementsWireMock measurementsWireMock;

    public ContractsV20240515Tests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
        measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    public async Task Bla()
    {
        var subject = Guid.NewGuid().ToString();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId: orgId.ToString(), apiVersion: ApiVersions.Version20240515);

        var contract = new CreateContract
        {
            GSRN = GsrnHelper.GenerateRandom(),
            StartDate = DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds(),
            EndDate = DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeSeconds()
        };

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?orgId={orgId}", new CreateContracts([contract]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
