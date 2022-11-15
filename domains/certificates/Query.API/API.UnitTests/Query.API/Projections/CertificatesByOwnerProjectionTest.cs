using System;
using System.Collections.Generic;
using System.Numerics;
using API.Query.API.Projections;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Query.API.Projections;

public class CertificatesByOwnerProjectionTest
{
    public static IEnumerable<object[]> Cases
    {
        get
        {
            var certificateId = Guid.Parse("F3DC97BD-108A-45BD-9532-D67172ECE252");

            var createdEvent = new ProductionCertificateCreated(
                CertificateId: certificateId,
                GridArea: "gridArea",
                Period: new Period(DateFrom: 1, DateTo: 42),
                Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
                MeteringPointOwner: "meteringPointOwner",
                ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn", R: BigInteger.Zero),
                ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

            var issuedEvent = new ProductionCertificateIssued(
                CertificateId: certificateId,
                MeteringPointOwner: "meteringPointOwner",
                GSRN: "gsrn");

            var rejectedEvent = new ProductionCertificateRejected(
                CertificateId: certificateId,
                Reason: "foo",
                MeteringPointOwner: "meteringPointOwner",
                GSRN: "gsrn");

            return new List<object[]>
            {
                new object[]
                {
                    (Action<CertificatesByOwnerProjection, CertificatesByOwnerView>) ((projection, view) => projection.Apply(createdEvent, view)),
                    new CertificatesByOwnerView
                    {
                        Owner = "meteringPointOwner",
                        Certificates = new Dictionary<Guid, CertificateView>
                        {
                            {
                                certificateId,
                                new CertificateView
                                {
                                    DateFrom = 1, DateTo = 42, GSRN = "gsrn", Quantity = 42, FuelCode = "F00000000", TechCode = "T010000", Status = CertificateStatus.Creating
                                }
                            }
                        }
                    }
                },
                new object []
                {
                    (Action<CertificatesByOwnerProjection, CertificatesByOwnerView>) ((projection, view) =>
                    {
                        projection.Apply(createdEvent, view);
                        projection.Apply(issuedEvent, view);
                    }),
                    new CertificatesByOwnerView
                    {
                        Owner = "meteringPointOwner",
                        Certificates = new Dictionary<Guid, CertificateView>
                        {
                            {
                                certificateId,
                                new CertificateView
                                {
                                    DateFrom = 1, DateTo = 42, GSRN = "gsrn", Quantity = 42, FuelCode = "F00000000", TechCode = "T010000", Status = CertificateStatus.Issued
                                }
                            }
                        }
                    }
                },
                new object []
                {
                    (Action<CertificatesByOwnerProjection, CertificatesByOwnerView>) ((projection, view) =>
                    {
                        projection.Apply(createdEvent, view);
                        projection.Apply(rejectedEvent, view);
                    }),
                    new CertificatesByOwnerView
                    {
                        Owner = "meteringPointOwner",
                        Certificates = new Dictionary<Guid, CertificateView>
                        {
                            {
                                certificateId,
                                new CertificateView
                                {
                                    DateFrom = 1, DateTo = 42, GSRN = "gsrn", Quantity = 42, FuelCode = "F00000000", TechCode = "T010000", Status = CertificateStatus.Rejected
                                }
                            }
                        }
                    }
                }
            };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Apply_EventsApplied_ViewAsExpected(Action<CertificatesByOwnerProjection, CertificatesByOwnerView> apply, CertificatesByOwnerView expected)
    {
        var projection = new CertificatesByOwnerProjection();
        var view = new CertificatesByOwnerView();

        apply(projection, view);

        view.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Apply_TwoCreatedEvents_HasTwoCertificates()
    {
        var projection = new CertificatesByOwnerProjection();
        var view = new CertificatesByOwnerView();

        var createdEvent1 = new ProductionCertificateCreated(
            CertificateId: Guid.NewGuid(),
            GridArea: "gridArea",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            MeteringPointOwner: "meteringPointOwner",
            ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn1", R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

        var createdEvent2 = new ProductionCertificateCreated(
            CertificateId: Guid.NewGuid(),
            GridArea: "gridArea",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            MeteringPointOwner: "meteringPointOwner",
            ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn2", R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

        projection.Apply(createdEvent1, view);
        projection.Apply(createdEvent2, view);

        view.Certificates.Should().HaveCount(2);
    }
}
