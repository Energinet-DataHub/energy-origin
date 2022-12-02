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

public class DbTest : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public DbTest(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer)
    {
        this.factory = factory;

        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
    }

    [Fact]
    public async Task NoData()
    {
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetAsync("certificates");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HasData()
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
