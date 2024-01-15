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

namespace Tests.MeteringPoints.Api.v2024_01_10;

[UsesVerify]
public class MeteringPointControllerTests : MeasurementsTestBase
{
    public MeteringPointControllerTests(TestServerFixture<Startup> serverFixture)
    : base(serverFixture)
    {
    }

    [Fact]
    public async Task Unauthorized()
    {
        var client = _serverFixture.CreateUnauthenticatedClient();

        var response = await client.GetAsync("api/meteringpoints");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMeteringPoints()
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
                        StreetName = "Street",
                        BuildingNumber = "1",
                        FloorId = "1",
                        RoomId = "1",
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

        var response = await client.GetFromJsonAsync<GetMeteringPointsResponse>("api/meteringpoints");

        response.Should().NotBeNull();
        var settings = new VerifySettings();
        settings.DontScrubGuids();
        await Verifier.Verify(response, settings);
    }
}
