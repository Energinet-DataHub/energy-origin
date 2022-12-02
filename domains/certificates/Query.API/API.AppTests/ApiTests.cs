using System;
using System.Linq;
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
using MassTransit;
using VerifyXunit;
using Xunit;

namespace API.AppTests;

[UsesVerify]
public class ApiTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    MasterData masterDataTemplate = new(
        GSRN: "GSRN",
        GridArea: "GridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology("foo", "bar"),
        MeteringPointOwner: "",
        MeteringPointOnboardedStartDate: DateTimeOffset.Parse("2022-01-01T00:00Z"));

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
        var gsrn = Guid.NewGuid().ToString();

        factory.AddMasterData(masterDataTemplate with { MeteringPointOwner = subject, GSRN = gsrn});

        var bus = factory.GetMassTransitBus();

        await bus.Publish(new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: DateTimeOffset.Parse("2022-06-01T00:00Z").ToUnixTimeSeconds(),
            DateTo: DateTimeOffset.Parse("2022-06-01T01:00Z").ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured));

        using var client = factory.CreateAuthenticatedClient(subject);

        var apiResponse = await client.RepeatedlyGetUntil("certificates", res => res.StatusCode == HttpStatusCode.OK);
        var certificateList = await apiResponse.Content.ReadFromJsonAsync<CertificateList>();
        await Verifier.Verify(certificateList);
    }

    [Fact]
    public async Task GetList_FiveMeasurementAddedToBus_ReturnsList()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = Guid.NewGuid().ToString();

        factory.AddMasterData(masterDataTemplate with { MeteringPointOwner = subject, GSRN = gsrn });

        var bus = factory.GetMassTransitBus();

        var dateStart = DateTimeOffset.Parse("2022-06-01T00:00Z");
        var list = Enumerable.Range(0, 5)
            .Select(i => new EnergyMeasuredIntegrationEvent(
                GSRN: gsrn,
                DateFrom: dateStart.AddHours(i).ToUnixTimeSeconds(),
                DateTo: dateStart.AddHours(i + 1).ToUnixTimeSeconds(),
                Quantity: 42+i,
                Quality: MeasurementQuality.Measured))
            .ToList();

        await bus.PublishBatch(list);

        using var client = factory.CreateAuthenticatedClient(subject);

        var certificateList = await client.RepeatedlyGetUntil<CertificateList>("certificates", res => res.Result.Count() == 5);
        await Verifier.Verify(certificateList);
    }
}
