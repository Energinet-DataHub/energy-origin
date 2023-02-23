using System;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using API.AppTests.Infrastructure;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class TransferTest :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public TransferTest(QueryApiWebApplicationFactory factory, MartenDbContainer martenDbContainer)
    {
        this.factory = factory;
        this.factory.MartenConnectionString = martenDbContainer.ConnectionString;
    }

    [Fact]
    public async Task Test()
    {
        var owner1 = Guid.NewGuid().ToString();
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

        var documentStore = factory.GetDocumentStore();
        await using var documentSession = documentStore.OpenSession();

        documentSession.Events.StartStream(createdEvent.CertificateId, createdEvent, issuedEvent);
        await documentSession.SaveChangesAsync();

        var certificates = await client.GetAsync("api/certificates");
        certificates.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
