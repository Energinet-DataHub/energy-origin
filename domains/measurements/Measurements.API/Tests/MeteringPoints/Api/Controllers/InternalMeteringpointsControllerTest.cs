using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api.Controllers.Internal;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Extensions;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Meteringpoint.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace Tests.MeteringPoints.Api.Controllers;

public class InternalMeteringpointsControllerTest : IClassFixture<CustomMeterPointWebApplicationFactory<Startup>>, IClassFixture<PostgresContainer>
{
    private readonly CustomMeterPointWebApplicationFactory<Startup> _factory;

    public InternalMeteringpointsControllerTest(CustomMeterPointWebApplicationFactory<Startup> factory, PostgresContainer postgresContainer)
    {
        var databaseInfo = postgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(databaseInfo.ConnectionString, typeof(Startup).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
        factory.ConnectionString = databaseInfo.ConnectionString;
        _factory = factory;
        _factory.Start();
    }


    [Fact]
    public async Task GetMeteringPoints_WithOrganizationId_ReturnsListOfMeteringpoints()
    {
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(Guid.Parse(_factory.AdminPortalEnterpriseAppRegistrationObjectId), orgId);

        SetupMockedMeteringPointsResponse(orgId);

        var response = await client.GetAsync(
            "api/measurements/admin-portal/internal-meteringpoints?organizationId=" + orgId,
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetInternalMeteringPointsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        await Verifier.Verify(result);
    }


    [Fact]
    public async Task GetMeteringPoints_ListOfOrganizationIdsNoMeteringpoints_ReturnsEmptyList()
    {
        var orgId = Guid.NewGuid();

        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest()
        {
            Actor = "Ett-admin-portal",
            Subject = orgId.ToString()
        },
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse());

        var client = _factory.CreateB2CAuthenticatedClient(Guid.Parse(_factory.AdminPortalEnterpriseAppRegistrationObjectId), orgId);

        var response = await client.GetAsync(
            "api/measurements/admin-portal/internal-meteringpoints?organizationId=" + orgId,
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetInternalMeteringPointsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result!.Result);
    }

    private void SetupMockedMeteringPointsResponse(Guid orgId)
    {
        var mockedResponse = new MeteringPointsResponse
        {
            MeteringPoints =
            {
                new MeteringPoint
                {
                    MeteringPointId = "1234567890123456",
                    TypeOfMp = "E17",
                    SubtypeOfMp = "D01",
                    StreetName = "Street",
                    BuildingNumber = "1",
                    FloorId = "1",
                    RoomId = "1",
                    CityName = "City",
                    Postcode = "1234",
                    AssetType = "E17",
                    Capacity = "12345678",
                    PhysicalStatusOfMp = "E22",
                    ConsumerCvr = "12345678",
                    MunicipalityCode = "101", // Copenhagen
                    CitySubDivisionName = "vesterbro",
                    MeteringGridAreaId = "932"
                },
                new MeteringPoint
                {
                    MeteringPointId = "12345678901234512",
                    TypeOfMp = "E17",
                    SubtypeOfMp = "D01",
                    StreetName = "Street",
                    BuildingNumber = "1",
                    FloorId = "1",
                    RoomId = "1",
                    CityName = "City",
                    Postcode = "1234",
                    AssetType = "E17",
                    Capacity = "12345678",
                    PhysicalStatusOfMp = "E22",
                    ConsumerCvr = "12345678",
                    MunicipalityCode = "101", // Copenhagen
                    CitySubDivisionName = "vesterbro",
                    MeteringGridAreaId = "932"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest()
        {
            Actor = "Ett-admin-portal",
            Subject = orgId.ToString()
        },
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockedResponse);
    }
}
