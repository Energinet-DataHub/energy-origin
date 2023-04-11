using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Helpers;
using API.AppTests.Infrastructure;
using API.AppTests.Infrastructure.WriteToConsole;
using API.AppTests.Mocks;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using MeasurementEvents;
using Xunit;

namespace API.AppTests;

[Collection(StartupCollection.Name)]
[WriteToConsole]
public sealed class TransferTests :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<DataSyncWireMock>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public TransferTests(
        QueryApiWebApplicationFactory factory,
        MartenDbContainer martenDbContainer,
        RabbitMqContainer rabbitMqContainer,
        DataSyncWireMock dataSyncWireMock)
    {
        this.dataSyncWireMock = dataSyncWireMock;
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
    }

    [Fact]
    public async Task can_successfully_transfer()
    {
        var (owner1, owner1Client, owner2, owner2Client, certificateId) = await Setup();

        var body = new { CertificateId = certificateId, Source = owner1, Target = owner2 };
        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/transfer", body);

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

        var bodyWithWrongOwnerAsSource = new { CertificateId = certificateId, Source = owner2, Target = owner1 };

        var transferResult =
            await owner1Client.PostAsJsonAsync("api/certificates/transfer", bodyWithWrongOwnerAsSource);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_for_unknown_certificate_id()
    {
        var (owner1, owner1Client, owner2, _, _) = await Setup();

        var unknownCertificateId = Guid.NewGuid();

        var body = new { CertificateId = unknownCertificateId, Source = owner1, Target = owner2 };

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/transfer", body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_when_transferring_to_same_owner()
    {
        var (owner1, owner1Client, _, _, certificateId) = await Setup();

        var bodyWithSameOwner = new { CertificateId = certificateId, Source = owner1, Target = owner1 };

        var transferResult = await owner1Client.PostAsJsonAsync("api/certificates/transfer", bodyWithSameOwner);

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

        var certificateListForOwner1 =
            await owner1Client.RepeatedlyGetUntil<CertificateList>("api/certificates", res => res.Result.Any());

        var certificateId = certificateListForOwner1.Result.Single().Id;

        return (owner1, owner1Client, owner2, owner2Client, certificateId);
    }
}
