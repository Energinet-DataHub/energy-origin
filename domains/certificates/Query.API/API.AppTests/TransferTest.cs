using System;
using System.Net;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.IntegrationTest.Infrastructure;
using API.Query.API.ApiModels.Requests;
using API.RabbitMq.Configurations;
using Baseline.ImTools;
using CertificateEvents;
using CertificateEvents.Primitives;
using Contracts.Transfer;
using FluentAssertions;
using WireMock.ResponseBuilders;
using Xunit;

namespace API.AppTests;

public class TransferTest :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public TransferTest(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer, RabbitMqContainer rabbitMqContainer)
    {
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
        this.factory.RabbitMqSetup = new RabbitMqOptions
        {
            Username = rabbitMqContainer.Username,
            Password = rabbitMqContainer.Password,
            Host = rabbitMqContainer.Hostname,
            Port = rabbitMqContainer.Port
        };
    }

    [Fact]
    public async Task Test()
    {
        var owner1 = Guid.NewGuid().ToString();
        var owner2 = Guid.NewGuid().ToString();
        var client = factory.CreateAuthenticatedClient(owner1);

        var createdEvent = new ProductionCertificateCreated(
            CertificateId: Guid.NewGuid(),
            GridArea: "gridArea",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            MeteringPointOwner: owner1,
            ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn1", R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

        var issuedEvent = new ProductionCertificateIssued(
            createdEvent.CertificateId,
            createdEvent.MeteringPointOwner,
            createdEvent.ShieldedGSRN.Value);

        var transferredEvent = new CertificateTransferred(
            createdEvent.CertificateId,
            owner1,
            owner2,
            createdEvent.GridArea,
            createdEvent.Period,
            createdEvent.Technology,
            createdEvent.ShieldedGSRN,
            createdEvent.ShieldedQuantity);

        var documentStore = factory.GetDocumentStore();
        await using var documentSession = documentStore.OpenSession();

        documentSession.Events.StartStream(createdEvent.CertificateId, createdEvent, issuedEvent, transferredEvent);
        await documentSession.SaveChangesAsync();

        var certificates = await client.GetAsync("api/certificates");
        certificates.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TransferCertificate_Transfer_success()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var transferObject = new TransferCertificate()
        {
            CurrentOwner = "123456789",
            NewOwner = "987654321",
            CertificateId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("api/certificates/production/transfer", transferObject);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var respBody = await response.Content.ReadFromJsonAsync<TransferProductionCertificateResponse>();
        respBody?.Status.Should().Be("OK");
    }

    [Fact]
    public async Task TransferCertificate_transfer_SameOwner()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var transferObject = new TransferCertificate()
        {
            CurrentOwner = "123456789",
            NewOwner = "123456789",
            CertificateId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("api/certificates/production/transfer", transferObject);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferCertificate_transfer_OwnerEmpty()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var transferObject = new TransferCertificate()
        {
            CurrentOwner = "",
            NewOwner = "123456789",
            CertificateId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("api/certificates/production/transfer", transferObject);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var transferObject2 = new TransferCertificate()
        {
            CurrentOwner = "123456789",
            NewOwner = "",
            CertificateId = Guid.NewGuid()
        };

        var response2 = await client.PostAsJsonAsync("api/certificates/production/transfer", transferObject2);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }
}
