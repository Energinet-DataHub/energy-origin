using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API;
using API.MeteringPoints.Api;
using API.MeteringPoints.Api.Dto.Responses;
using API.MeteringPoints.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tests.Extensions;
using Tests.TestContainers;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Tests.MeteringPoints.Api;

public class MeteringPointControllerTests : IClassFixture<CustomMeterPointWebApplicationFactory<Startup>>,
    IClassFixture<PostgresContainer>
{
    private readonly CustomMeterPointWebApplicationFactory<Startup> _factory;

    public MeteringPointControllerTests(CustomMeterPointWebApplicationFactory<Startup> factory,
        PostgresContainer postgresContainer)

    {
        factory.ConnectionString = postgresContainer.ConnectionString;
        _factory = factory;
        _factory.Start();
    }

    [Fact]
    public async Task Unauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("api/measurements/meteringpoints");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NoMeteringPointsReturnsPendingRelation()
    {
        var mockedResponse = new Meteringpoint.V1.MeteringPointsResponse();

        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);

        var subject = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(subject.ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();
        response!.Status.Should().Be(RelationStatus.Pending);
    }

    [Fact]
    public async Task GetMeteringPoints()
    {
        var mockedResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
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
                    Capacity = "12345678"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);

        var subject = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(subject.ToString());

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_factory.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        dbContext.Database.EnsureCreated();

        dbContext.Relations.Add(new RelationDto()
        {
            SubjectId = subject,
            Status = RelationStatus.Created,
            Actor = Guid.NewGuid()
        });
        dbContext.SaveChanges();

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should()
            .Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Fact]
    public async Task GetMeteringPoints_GivenChildMp_ExpectChildMpOmitted()
    {
        var childTypeOfMp = "D01";

        var mockedResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
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
                    Capacity = "12345678"
                },
                new Meteringpoint.V1.MeteringPoint
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
                    Capacity = "12345678"
                }
            }
        };
        var clientMock = _factory.Services.GetRequiredService<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);

        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should()
            .Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should()
            .Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
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
        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(new Meteringpoint.V1.MeteringPointsResponse
            {
                MeteringPoints =
                {
                    new Meteringpoint.V1.MeteringPoint
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
                        Capacity = "12345678"
                    }
                }
            });


        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/measurements/meteringpoints");

        response.Should().NotBeNull();

        response!.Result.First().Address.Address1.Trim().Should().Be(response.Result.First().Address.Address1);
        response.Result.First().Address.Address2!.Trim().Should().Be(response.Result.First().Address.Address2);
        response.Result.First().Address.Address1.Should().NotContain("  ");
        response.Result.First().Address.Address2.Should().NotContain("  ");
    }
}
