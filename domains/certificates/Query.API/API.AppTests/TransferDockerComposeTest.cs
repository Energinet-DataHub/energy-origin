using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Helpers;
using API.AppTests.Infrastructure;
using API.AppTests.Mocks;
using API.Query.API.ApiModels.Responses;
using API.RabbitMq.Configurations;
using FluentAssertions;
using MeasurementEvents;
using Xunit;

namespace API.AppTests;

public class TransferDockerComposeTest : IClassFixture<QueryApiWebApplicationFactory>, IDisposable
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public TransferDockerComposeTest(QueryApiWebApplicationFactory factory)
    {
        dataSyncWireMock = new DataSyncWireMock(port: 9004);
        this.factory = factory;
        this.factory.MartenConnectionString =
            "host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;";
        this.factory.RabbitMqSetup = new RabbitMqOptions
        {
            Host = "localhost",
            Port = 5672,
            Username = "guest",
            Password = "guest"
        };
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
    }

    [Fact]
    public async Task Test()
    {
        var owner1 = Guid.NewGuid().ToString();
        var owner2 = Guid.NewGuid().ToString();

        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(owner1, gsrn, utcMidnight, dataSyncWireMock);

        var owner1Client = factory.CreateAuthenticatedClient(owner1);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var certificateListForOwner1BeforeTransfer =
            await owner1Client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Any());

        var certificateId = certificateListForOwner1BeforeTransfer.Result.Single().Id;

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            new { CertificateId = certificateId, CurrentOwner = owner1, NewOwner = owner2 });

        transferResult.StatusCode.Should().Be(HttpStatusCode.OK);

        var owner2Client = factory.CreateAuthenticatedClient(owner2);

        var certificateListForOwner2 = await owner2Client.GetFromJsonAsync<CertificateList>("api/certificates");

        certificateListForOwner2!.Result.Should().HaveCount(1);

        var certificateListResponseForOwner1AfterTransfer = await owner1Client.GetAsync("api/certificates");
        certificateListResponseForOwner1AfterTransfer.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    public void Dispose() => dataSyncWireMock.Dispose();
}
