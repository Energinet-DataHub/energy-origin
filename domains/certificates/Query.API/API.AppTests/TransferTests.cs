using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Extensions;
using API.AppTests.Factories;
using API.AppTests.Helpers;
using API.AppTests.Mocks;
using API.AppTests.Testcontainers;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using MeasurementEvents;
using Xunit;

namespace API.AppTests;

public sealed class TransferTests :
    TestBase,
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
        using var setup = await Setup.Create(factory, dataSyncWireMock);

        var body = new { setup.CertificateId, Source = setup.Owner1, Target = setup.Owner2 };
        using var transferResult = await setup.Owner1Client.PostAsJsonAsync("api/certificates/transfer", body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.OK);

        var certificateListForOwner2 = await setup.Owner2Client.GetFromJsonAsync<CertificateList>("api/certificates");

        certificateListForOwner2!.Result.Should().HaveCount(1);

        using var certificateListResponseForOwner1 = await setup.Owner1Client.GetAsync("api/certificates");

        certificateListResponseForOwner1.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task fails_when_transferring_in_the_wrong_direction()
    {
        using var setup = await Setup.Create(factory, dataSyncWireMock);

        var bodyWithWrongOwnerAsSource = new { setup.CertificateId, Source = setup.Owner2, Target = setup.Owner1 };

        using var transferResult =
            await setup.Owner1Client.PostAsJsonAsync("api/certificates/transfer", bodyWithWrongOwnerAsSource);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_for_unknown_certificate_id()
    {
        using var setup = await Setup.Create(factory, dataSyncWireMock);

        var unknownCertificateId = Guid.NewGuid();

        var body = new { CertificateId = unknownCertificateId, Source = setup.Owner1, Target = setup.Owner2 };

        using var transferResult = await setup.Owner1Client.PostAsJsonAsync("api/certificates/transfer", body);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task fails_when_transferring_to_same_owner()
    {
        using var setup = await Setup.Create(factory, dataSyncWireMock);

        var bodyWithSameOwner = new { setup.CertificateId, Source = setup.Owner1, Target = setup.Owner1 };

        using var transferResult = await setup.Owner1Client.PostAsJsonAsync("api/certificates/transfer", bodyWithSameOwner);

        transferResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class Setup : IDisposable
    {
        public string Owner1 { get; }
        public HttpClient Owner1Client { get; }
        public string Owner2 { get; }
        public HttpClient Owner2Client { get; }
        public Guid CertificateId { get; }

        private Setup(string owner1, HttpClient owner1Client, string owner2, HttpClient owner2Client, Guid certificateId)
        {
            Owner1 = owner1;
            Owner1Client = owner1Client;
            Owner2 = owner2;
            Owner2Client = owner2Client;
            CertificateId = certificateId;
        }

        /// <summary>
        /// Creates a contract for "owner1" and then publishes a measurement. Wait for a certificate to be generated for the measurement
        /// </summary>
        public static async Task<Setup> Create(QueryApiWebApplicationFactory factory, DataSyncWireMock dataSyncWireMock)
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

            return new Setup(owner1, owner1Client, owner2, owner2Client, certificateId);
        }

        public void Dispose()
        {
            Owner1Client.Dispose();
            Owner2Client.Dispose();
        }
    }
}
