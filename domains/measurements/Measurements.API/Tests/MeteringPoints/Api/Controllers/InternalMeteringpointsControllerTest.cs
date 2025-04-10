using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api.Controllers.Internal;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Meteringpoint.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tests.Extensions;
using VerifyXunit;
using Xunit;
using MeteringPoint = Meteringpoint.V1.MeteringPoint;

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
    public async Task GetMeteringPoints_ListOfOrganizationIds_ReturnsListOfMeteringpoints()
    {
        var orgId = Guid.NewGuid();
        var orgId1 = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(Guid.Parse(_factory.AdminPortalEnterpriseAppRegistrationObjectId), orgId);

        SetupMockedMeteringPointsResponse(orgId, orgId1);

        var response = await client.PostAsJsonAsync(
            "api/measurements/admin-portal/internal-meteringpoints",
            new List<Guid>() { orgId, orgId1 },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetInternalMeteringPointsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        await Verifier.Verify(result);
    }

    [Fact]
    public async Task GetMeteringPoints_ListOfOrganizationDatahubThrowsException_NoMeteringpointsIsAdded()
    {
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(Guid.Parse(_factory.AdminPortalEnterpriseAppRegistrationObjectId), orgId);

        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest()
                {
                    Actor = "Ett-admin-portal",
                    Subject = orgId.ToString()
                },
                cancellationToken: Arg.Any<CancellationToken>())
            .Throws(new Exception());

        var response = await client.PostAsJsonAsync(
            "api/measurements/admin-portal/internal-meteringpoints",
            new List<Guid>() { orgId },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetInternalMeteringPointsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result!.Result);
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

        var response = await client.PostAsJsonAsync(
            "api/measurements/admin-portal/internal-meteringpoints",
            new List<Guid>() { orgId },
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetInternalMeteringPointsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result!.Result);
    }

    private void SetupMockedMeteringPointsResponse(Guid orgId, Guid orgId1)
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
                    ConsumerCvr = "12345678"
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
                    ConsumerCvr = "12345678"
                }
            }
        };
        var mockedResponse1 = new MeteringPointsResponse
        {
            MeteringPoints =
            {
                new MeteringPoint
                {
                    MeteringPointId = "12341236",
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
                    ConsumerCvr = "87654321"
                },
                new MeteringPoint
                {
                    MeteringPointId = "14",
                    TypeOfMp = "E18",
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
                    ConsumerCvr = "87654321"
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
        clientMock.GetOwnedMeteringPointsAsync(new OwnedMeteringPointsRequest()
        {
            Actor = "Ett-admin-portal",
            Subject = orgId1.ToString()
        },
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockedResponse1);
    }
}
