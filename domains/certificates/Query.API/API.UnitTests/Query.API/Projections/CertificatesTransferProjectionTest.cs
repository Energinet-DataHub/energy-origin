using System;
using System.Numerics;
using API.Query.API.Projections;
using API.Query.API.Projections.Views;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Query.API.Projections;

public class CertificatesTransferProjectionTest
{
    [Fact]
    public void Apply_Transferred_CertificateRemovedFromSourceToTarget()
    {
        var certificatesTransferProjection = new CertificatesTransferProjection();
        var certificatesByOwnerProjection = new CertificatesByOwnerProjection();
        var viewOwner1 = new CertificatesByOwnerView { Owner = "owner1" };
        var viewOwner2 = new CertificatesByOwnerView { Owner = "owner2" };

        var createdEvent = new ProductionCertificateCreated(
            CertificateId: Guid.NewGuid(),
            GridArea: "gridArea",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            MeteringPointOwner: "owner1",
            ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn1", R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

        var transferredEvent = new ProductionCertificateTransferred(
            createdEvent.CertificateId,
            "owner1",
            "owner2");

        certificatesByOwnerProjection.Apply(createdEvent, viewOwner1);
        //
        // certificatesTransferProjection.Apply(transferredEvent, viewOwner1);
        //
        // certificatesTransferProjection.Apply(transferredEvent, viewOwner2);

        viewOwner1.Certificates.Should().BeEmpty();
        viewOwner2.Certificates.Should().HaveCount(1);
    }
}
