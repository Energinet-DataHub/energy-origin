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
using API.IntegrationTest.Infrastructure;
using API.Query.API.ApiModels.Responses;
using API.RabbitMq.Configurations;
using FluentAssertions;
using MeasurementEvents;
using Xunit;

namespace API.AppTests;

public sealed class TransferTests :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>,
    IDisposable
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public TransferTests(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer,
        RabbitMqContainer rabbitMqContainer)
    {
        dataSyncWireMock = new DataSyncWireMock(port: 9004);
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqSetup = new RabbitMqOptions
        {
            Username = rabbitMqContainer.Username,
            Password = rabbitMqContainer.Password,
            Host = rabbitMqContainer.Hostname,
            Port = rabbitMqContainer.Port
        };
    }

    [Fact]
    public async Task can_successfully_transfer()
    {
        var (owner1, owner1Client, owner2, owner2Client, certificateId) = await Setup();

        var body = new { CertificateId = certificateId, CurrentOwner = owner1, NewOwner = owner2 };
        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer", body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.OK);

        var certificateListForOwner2 = await owner2Client.GetFromJsonAsync<CertificateList>("api/certificates");

        certificateListForOwner2!.Result.Should().HaveCount(1);

        var certificateListResponseForOwner1 = await owner1Client.GetAsync("api/certificates");
        certificateListResponseForOwner1.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task fails_when_transferring_in_the_wrong_direction()
    {
        var (owner1, owner1Client, owner2, _, certificateId) = await Setup();

        var bodyWithOwnersSwitched = new { CertificateId = certificateId, CurrentOwner = owner2, NewOwner = owner1 };

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            bodyWithOwnersSwitched);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_for_unknown_certificate_id()
    {
        var (owner1, owner1Client, owner2, _, _) = await Setup();

        var unknownCertificateId = Guid.NewGuid();

        var body = new { CertificateId = unknownCertificateId, CurrentOwner = owner1, NewOwner = owner2 };

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_when_transferring_to_same_owner()
    {
        var (owner1, owner1Client, _, _, certificateId) = await Setup();

        var bodyWithSameOwner = new { CertificateId = certificateId, CurrentOwner = owner1, NewOwner = owner1 };

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/production/transfer",
            bodyWithSameOwner);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Creates a contract for "owner1" and then publishes a measurement. Wait for a certificate to be generated for the measurement
    /// </summary>
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
