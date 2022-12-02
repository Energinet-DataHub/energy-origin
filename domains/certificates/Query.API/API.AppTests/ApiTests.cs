using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Infrastructure;
using API.MasterDataService;
using API.Query.API.ApiModels;
using CertificateEvents.Primitives;
using FluentAssertions;
using IntegrationEvents;
using Xunit;

namespace API.AppTests;

public class ApiTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public ApiTests(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer)
    {
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
    }

    [Fact]
    public async Task GetList_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var client = factory.CreateUnauthenticatedClient();
        var certificatesResponse = await client.GetAsync("certificates");

        Assert.Equal(HttpStatusCode.Unauthorized, certificatesResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetAsync("certificates");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetList_MeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();

        factory.AddMasterData(new MasterData(
            GSRN: "GSRN",
            GridArea: "GridArea",
            Type: MeteringPointType.Production,
            Technology: new Technology("foo", "bar"),
            MeteringPointOwner: subject,
            MeteringPointOnboardedStartDate: DateTimeOffset.Now.AddYears(-1)));

        var bus = factory.GetMassTransitBus();

        await bus.Publish(new EnergyMeasuredIntegrationEvent(
            GSRN: "GSRN",
            DateFrom: DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds(),
            DateTo: DateTimeOffset.Now.ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured));

        using var client = factory.CreateAuthenticatedClient(subject);

        var apiResponse = await client.RepeatedlyGetUntil("certificates", res => res.StatusCode == HttpStatusCode.OK);
        var certificateList = await apiResponse.Content.ReadFromJsonAsync<CertificateList>();
        certificateList?.Result.Should().HaveCount(1);
    }
}
