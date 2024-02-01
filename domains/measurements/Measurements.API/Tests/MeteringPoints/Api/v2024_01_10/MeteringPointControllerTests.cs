using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses;
using FluentAssertions;
using NSubstitute;
using VerifyTests;
using VerifyXunit;
using Xunit;
using Tests.Extensions;
using API;
using Microsoft.Extensions.DependencyInjection;
using Tests.Fixtures;
using System.Linq;
using System.Net;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;

namespace Tests.MeteringPoints.Api.v2024_01_10;

public class MeteringPointControllerTests : MeasurementsTestBase, IDisposable
{
    public MeteringPointControllerTests(TestServerFixture<Startup> serverFixture)
        : base(serverFixture)
    {
    }

    public void Dispose()
    {
        _serverFixture.RefreshHostAndGrpcChannelOnNextClient();
    }

    [Fact]
    public async Task Unauthorized()
    {
        var client = _serverFixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("api/new-meteringpoints");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMeteringPoints()
    {
        var clientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

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
                    AssetType = "E17"
                }
            }
        };

        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);
        _serverFixture.ConfigureTestServices += services =>
        {
            var mpClient = services.Single(d =>
                d.ServiceType == typeof(Meteringpoint.V1.Meteringpoint.MeteringpointClient));
            services.Remove(mpClient);
            services.AddSingleton(clientMock);
        };

        var client = _serverFixture.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/new-meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should().Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should().Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
    }

    [Fact]
    public async Task GetMeteringPoints_GivenChildMp_ExpectChildMpOmitted()
    {
        var childTypeOfMp = "D01";
        var clientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

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
                    AssetType = "E17"
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
                    AssetType = "E17"
                }
            }
        };

        clientMock.GetOwnedMeteringPointsAsync(Arg.Any<Meteringpoint.V1.OwnedMeteringPointsRequest>())
            .Returns(mockedResponse);
        _serverFixture.ConfigureTestServices += services =>
        {
            var mpClient = services.Single(d =>
                d.ServiceType == typeof(Meteringpoint.V1.Meteringpoint.MeteringpointClient));
            services.Remove(mpClient);
            services.AddSingleton(clientMock);
        };

        var client = _serverFixture.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/new-meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
        response!.Result.First().SubMeterType.Should().Be(MeteringPoint.GetSubMeterType(mockedResponse.MeteringPoints.First().SubtypeOfMp));
        response.Result.First().Type.Should().Be(MeteringPoint.GetMeterType(mockedResponse.MeteringPoints.First().TypeOfMp));
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

    public async void EmptyAddressInformation_GetMeteringPoint_AddressLinesWithoutWhiteSpace(string streetName, string buildingNumber, string floor, string room)
    {
        var clientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

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
                        AssetType = "E17"
                    }
                }
            });
        _serverFixture.ConfigureTestServices += services =>
        {
            var mpClient = services.Single(d => d.ServiceType == typeof(Meteringpoint.V1.Meteringpoint.MeteringpointClient));
            services.Remove(mpClient);
            services.AddSingleton(clientMock);
        };

        var client = _serverFixture.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/new-meteringpoints");

        response.Should().NotBeNull();

        response!.Result.First().Address.Address1.Trim().Should().Be(response.Result.First().Address.Address1);
        response.Result.First().Address.Address2!.Trim().Should().Be(response.Result.First().Address.Address2);
        response.Result.First().Address.Address1.Should().NotContain("  ");
        response.Result.First().Address.Address2.Should().NotContain("  ");
    }
}
