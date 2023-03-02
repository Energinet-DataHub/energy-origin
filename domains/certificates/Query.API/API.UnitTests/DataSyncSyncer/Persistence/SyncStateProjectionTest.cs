using System;
using System.Numerics;
using API.DataSyncSyncer.Persistence;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.DataSyncSyncer.Persistence;

public class SyncStateProjectionTest
{
    private readonly ProductionCertificateCreated createdEvent = new(
        CertificateId: Guid.NewGuid(),
        GridArea: "gridArea",
        Period: new Period(DateFrom: 1, DateTo: 42),
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn", R: BigInteger.Zero),
        ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

    [Fact]
    public void can_update_view_on_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        projection.Apply(createdEvent with { Period = new Period(1900, 2000) }, view);

        view.SyncDateTo.Should().Be(2000);
    }

    [Fact]
    public void will_not_update_when_period_is_before_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        projection.Apply(createdEvent with { Period = new Period(1900, 2000) }, view);
        projection.Apply(createdEvent with { Period = new Period(1800, 1900) }, view);

        view.SyncDateTo.Should().Be(2000);
    }

    [Fact]
    public void will_update_when_period_is_after_first_event()
    {
        var projection = new SyncStateProjection();
        var view = new SyncStateView();

        projection.Apply(createdEvent with { Period = new Period(1900, 2000) }, view);
        projection.Apply(createdEvent with { Period = new Period(2000, 2100) }, view);

        view.SyncDateTo.Should().Be(2100);
    }
}
