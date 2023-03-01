using System;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using API.IntegrationTest.Infrastructure;
using API.RabbitMq.Configurations;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

//TODO: Delete this test
public class TestForDevelopment :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public TestForDevelopment(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer, RabbitMqContainer rabbitMqContainer)
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

        var transferredEvent = new ProductionCertificateTransferred(
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
        certificates.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
