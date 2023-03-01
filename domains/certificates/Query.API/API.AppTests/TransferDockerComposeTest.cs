using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public async Task successfully_transfer()
    {
        var (owner1, owner1Client, owner2, owner2Client, certificateId) = await Setup();

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            new { CertificateId = certificateId, CurrentOwner = owner1, NewOwner = owner2 });

        transferResult.StatusCode.Should().Be(HttpStatusCode.OK);

        var certificateListForOwner2 = await owner2Client.GetFromJsonAsync<CertificateList>("api/certificates");

        certificateListForOwner2!.Result.Should().HaveCount(1);

        var certificateListResponseForOwner1AfterTransfer = await owner1Client.GetAsync("api/certificates");
        certificateListResponseForOwner1AfterTransfer.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task transfer_the_wrong_direction()
    {
        var (owner1, owner1Client, owner2, _, certificateId) = await Setup();

        var body = new { CertificateId = certificateId, CurrentOwner = owner2, NewOwner = owner1 };
        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer", body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task unknown_certificate_id()
    {
        var (owner1, owner1Client, owner2, _, _) = await Setup();

        var unknownCertificateId = Guid.NewGuid();

        var body = new { CertificateId = unknownCertificateId, CurrentOwner = owner1, NewOwner = owner2 };
        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task transfer_to_same_owner()
    {
        var (owner1, owner1Client, _, _, certificateId) = await Setup();

        var body = new { CertificateId = certificateId, CurrentOwner = owner1, NewOwner = owner1 };
        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Creates a contract and then publishes a measurement. Wait for a certificate to be generated for the measurement
    /// </summary>
    /// <returns></returns>
    private async Task<(string owner1, HttpClient owner1Client, string owner2, HttpClient, Guid certificateId)> Setup()
    {
        var owner1 = Guid.NewGuid().ToString();
        var owner2 = Guid.NewGuid().ToString();

        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(owner1, gsrn, utcMidnight, dataSyncWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var owner1Client = factory.CreateAuthenticatedClient(owner1);
        var owner2Client = factory.CreateAuthenticatedClient(owner2);

        var certificateListForOwner1BeforeTransfer =
            await owner1Client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Any());

        var certificateId = certificateListForOwner1BeforeTransfer.Result.Single().Id;

        return (owner1, owner1Client, owner2, owner2Client, certificateId);
    }

    public void Dispose() => dataSyncWireMock.Dispose();
}
