using System;
using System.Collections.Generic;
using System.Linq;
using API.MeasurementsSyncer;
using DataContext.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V1;
using FluentAssertions;
using Measurements.V1;
using Meteringpoint.V1;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class EnergyMeasuredIntegrationEventMapperTest
{
    private readonly EnergyMeasuredIntegrationEventMapper _sut = new();

    [Fact]
    public void GivenEmptyList_WhenMapping_NoEventsAreMapped()
    {
        var gsrn = Any.Gsrn();
        var start = DateTimeOffset.Now.AddDays(-1);
        var mappedEvents = _sut.MapToIntegrationEvents(new MeteringPoint(),
            new MeteringPointSyncInfo(gsrn, start, Guid.NewGuid().ToString(), MeteringPointType.Production, "DK1", Guid.NewGuid(), Any.Technology()),
            new List<Measurement>());

        mappedEvents.Should().BeEmpty();
    }

    [Fact]
    public void GivenMeasurement_WhenMapping_EventIsReturned()
    {
        // Given measurement
        var gsrn = Any.Gsrn();
        var start = DateTimeOffset.Now.AddDays(-1);
        var meteringPoint = Any.MeteringPointsResponse(gsrn).MeteringPoints.First();
        var measurement = Any.Measurement(gsrn, DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(), 5);
        var technology = Any.Technology();
        var recipientId = Guid.NewGuid();
        var syncInfo = new MeteringPointSyncInfo(gsrn, start, Guid.NewGuid().ToString(), MeteringPointType.Production, "DK1", recipientId,
            technology);

        // When mapping to event
        var mappedEvents = _sut.MapToIntegrationEvents(meteringPoint, syncInfo, new List<Measurement> { measurement });

        // Properties are mapped
        mappedEvents.Should().ContainSingle();
        var evt = mappedEvents.First();
        evt.Technology.AibTechCode.Should().Be(technology.TechCode);
        evt.Technology.AibFuelCode.Should().Be(technology.FuelCode);
        evt.GridArea.Should().Be(syncInfo.GridArea);
        evt.Capacity.Should().Be(meteringPoint.Capacity);
        evt.Address.Should().NotBeEmpty();
        evt.Quantity.Should().Be(measurement.Quantity);
        evt.DateFrom.Should().Be(measurement.DateFrom);
        evt.DateTo.Should().Be(measurement.DateTo);
        evt.MeterType.Should().Be(syncInfo.MeteringPointType == MeteringPointType.Consumption ? MeterType.Consumption : MeterType.Production);
        evt.RecipientId.Should().Be(recipientId);
        evt.GSRN.Should().Be(gsrn.Value);
    }
}
