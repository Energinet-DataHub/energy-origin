using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Models;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Meteringpoint.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tests.Extensions;
using VerifyTests;
using VerifyXunit;
using Xunit;
using MeteringPoint = Meteringpoint.V1.MeteringPoint;

namespace Tests.MeteringPoints.Api;

public class MeteringPointControllerTests : IClassFixture<CustomMeterPointWebApplicationFactory<Startup>>, IClassFixture<PostgresContainer>
{
    private readonly CustomMeterPointWebApplicationFactory<Startup> _factory;

    public MeteringPointControllerTests(CustomMeterPointWebApplicationFactory<Startup> factory, PostgresContainer postgresContainer)
    {
        var databaseInfo = postgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(databaseInfo.ConnectionString, typeof(Startup).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
        factory.ConnectionString = databaseInfo.ConnectionString;
        _factory = factory;
        _factory.Start();
    }

    [Fact]
    public async Task Unauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"api/measurements/meteringpoints?organizationId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NoMeteringPointsReturnsPendingRelation()
    {
        var mockedResponse = new MeteringPointsResponse();

        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(mockedResponse);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(subject, orgId);

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>($"api/measurements/meteringpoints?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response!.Status.Should().Be(RelationStatus.Pending);
    }

    [Fact]
    public async Task GetMeteringPoints()
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
                    MunicipalityCode = "101", // Copenhagen
                    CitySubDivisionName = "vesterbro",
                    MeteringGridAreaId = "932"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockedResponse);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(subject, orgId);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(contextOptions);

        dbContext.Relations.Add(new RelationDto()
        {
            SubjectId = subject,
            Status = RelationStatus.Created,
            Actor = Guid.NewGuid()
        });
        dbContext.SaveChanges();

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>($"api/measurements/meteringpoints?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);

        response!.Result.First().SubMeterType.Should()
            .Be(API.MeteringPoints.Api.Dto.Responses.MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(API.MeteringPoints.Api.Dto.Responses.MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Fact]
    public async Task GetMeteringPoints_GivenChildMp_ExpectChildMpOmitted()
    {
        var childTypeOfMp = "D01";

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
                    MunicipalityCode = "101", // Copenhagen
                    CitySubDivisionName = "vesterbro",
                    MeteringGridAreaId = "932"
                },
                new MeteringPoint
                {
                    MeteringPointId = "1234567890123457",
                    TypeOfMp = childTypeOfMp,
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
                    MunicipalityCode = "101", // Copenhagen
                    CitySubDivisionName = "vesterbro",
                    MeteringGridAreaId = "932"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(mockedResponse);

        var subjectId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(subjectId, orgId);

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>($"api/measurements/meteringpoints?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should()
            .Be(API.MeteringPoints.Api.Dto.Responses.MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(API.MeteringPoints.Api.Dto.Responses.MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Theory]
    [InlineData("streetName", "", "", "")]
    [InlineData("streetName", "buildingNo", "", "")]
    [InlineData("streetName", "buildingNo", "floor", "")]
    [InlineData("streetName", "buildingNo", "", "room")]
    [InlineData("streetName", "", "floor", "room")]
    [InlineData("streetName", "", "floor", "")]
    [InlineData("streetName", "", "", "room")]
    [InlineData("", "buildingNo", "floor", "room")]
    [InlineData("", "buildingNo", "", "room")]
    [InlineData("", "buildingNo", "floor", "")]
    [InlineData("", "", "floor", "room")]
    [InlineData("", "", "floor", "")]
    [InlineData("", "", "", "room")]
    [InlineData("", "", "", "")]
    [InlineData("streetName", "buildingNo", "floor", "room")]
    [InlineData("streetName", "    buildingNo", "floor", "room")]
    [InlineData("streetName", "buildingNo", "floor", "    room")]
    [InlineData("    streetName", "buildingNo", "floor", "room")]
    [InlineData("    streetName", "    buildingNo", "    floor", "    room")]
    [InlineData("streetName", "buildingNo", "    floor", "room")]
    public async Task EmptyAddressInformation_GetMeteringPoint_AddressLinesWithoutWhiteSpace(string streetName,
        string buildingNumber, string floor, string room)
    {
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new MeteringPointsResponse
            {
                MeteringPoints =
                {
                    new MeteringPoint
                    {
                        MeteringPointId = "1234567890123456",
                        TypeOfMp = "E17",
                        SubtypeOfMp = "D01",
                        StreetName = streetName,
                        BuildingNumber = buildingNumber,
                        FloorId = floor,
                        RoomId = room,
                        CityName = "City",
                        Postcode = "1234",
                        AssetType = "E17",
                        Capacity = "12345678",
                        MunicipalityCode = "101", // Copenhagen
                        CitySubDivisionName = "vesterbro",
                        MeteringGridAreaId = "932"
                    }
                }
            });

        var subjectId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(subjectId, orgId);

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>($"api/measurements/meteringpoints?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.Should().NotBeNull();

        response!.Result.First().Address.Address1.Trim().Should().Be(response.Result.First().Address.Address1);
        response.Result.First().Address.Address2!.Trim().Should().Be(response.Result.First().Address.Address2);
        response.Result.First().Address.Address1.Should().NotContain("  ");
        response.Result.First().Address.Address2.Should().NotContain("  ");
        response.Result.First().Address.MunicipalityCode.Should().Be("101");
        response.Result.First().Address.CitySubDivisionName.Should().Be("vesterbro");
    }
}
