using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Infrastructure;
using API.MasterDataService;
using API.Query.API.ApiModels;
using CertificateEvents.Primitives;
using FluentAssertions;
using IntegrationEvents;
using MassTransit;
using VerifyXunit;
using Xunit;

namespace API.AppTests;

[UsesVerify]
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
        var certificatesResponse = await client.GetAsync("api/certificates");

        Assert.Equal(HttpStatusCode.Unauthorized, certificatesResponse.StatusCode);
    }

    [Fact]
    public async Task GetList_NoCertificates_ReturnsNoContent()
    {
        var subject = Guid.NewGuid().ToString();

        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetAsync("api/certificates");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetList_MeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        const string gsrn = "GSRN-1";

        factory.AddMasterData(CreateMasterData(subject, gsrn));

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: DateTimeOffset.Parse("2022-06-01T00:00Z").ToUnixTimeSeconds(),
            DateTo: DateTimeOffset.Parse("2022-06-01T01:00Z").ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        using var client = factory.CreateAuthenticatedClient(subject);

        var certificateList = await client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Any());

        await Verifier.Verify(certificateList);
    }

    [Fact]
    public async Task GetList_FiveMeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        const string gsrn = "GSRN-2";

        factory.AddMasterData(CreateMasterData(subject, gsrn));

        var dateStart = DateTimeOffset.Parse("2022-06-01T00:00Z");
        var measurements = Enumerable.Range(0, 5)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: dateStart.AddHours(i).ToUnixTimeSeconds(),
                DateTo: dateStart.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42 + i,
                Quality: MeasurementQuality.Measured))
            .ToArray();

        await factory.GetMassTransitBus().PublishBatch(measurements);

        using var client = factory.CreateAuthenticatedClient(subject);

        var certificateList = await client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Count() == 5);

        await Verifier.Verify(certificateList);
    }

    private static MasterData CreateMasterData(string owner, string gsrn) => new(
        GSRN: gsrn,
        GridArea: "GridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology("foo", "bar"),
        MeteringPointOwner: owner,
        MeteringPointOnboardedStartDate: DateTimeOffset.Parse("2022-01-01T00:00Z"));

}
