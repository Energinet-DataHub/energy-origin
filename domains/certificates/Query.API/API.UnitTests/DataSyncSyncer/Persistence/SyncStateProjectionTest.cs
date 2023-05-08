using System;
using System.Numerics;
using API.DataSyncSyncer.Persistence;
using CertificateEvents;
using DomainCertificate;
using DomainCertificate.Primitives;
using DomainCertificate.ValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.DataSyncSyncer.Persistence;

public class SyncStateProjectionTest
{
    private readonly ProductionCertificateCreated createdEvent = new(
        CertificateId: Guid.NewGuid(),
        GridArea: "gridArea",
        Period: new Period(1, 42),
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        ShieldedGSRN: new ShieldedValue<string>(Shielded: "gsrn", R: BigInteger.Zero),
        ShieldedQuantity: new ShieldedValue<long>(Shielded: 42, R: BigInteger.Zero));

    [Fact]
    public void can_update_view_on_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        var period = new Period(1900, 2000);

        projection.Apply(createdEvent with { Period = period }, view);

        view.SyncDateTo.Should().Be(period.DateTo);
    }

    [Fact]
    public void will_not_update_when_period_is_before_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        var oldestPeriod = new Period(1800, 1900);
        var newestPeriod = new Period(1900, 2000);

        projection.Apply(createdEvent with { Period = newestPeriod }, view);
        projection.Apply(createdEvent with { Period = oldestPeriod }, view);

        view.SyncDateTo.Should().Be(newestPeriod.DateTo);
    }

    [Fact]
    public void will_update_when_period_is_after_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        var oldestPeriod = new Period(1800, 1900);
        var newestPeriod = new Period(1900, 2000);

        projection.Apply(createdEvent with { Period = oldestPeriod }, view);
        projection.Apply(createdEvent with { Period = newestPeriod }, view);

        view.SyncDateTo.Should().Be(newestPeriod.DateTo);
    }
}
