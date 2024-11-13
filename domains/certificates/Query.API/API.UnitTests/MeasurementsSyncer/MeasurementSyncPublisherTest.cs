using System;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using DataContext.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementSyncPublisherTest
{
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly EnergyMeasuredIntegrationEventMapper _mapper = new();
    private readonly ILogger<MeasurementSyncPublisher> _logger = Substitute.For<ILogger<MeasurementSyncPublisher>>();

    [Fact]
    public async Task GivenMappedEvents_WhenPublishing_EventsArePublished()
    {
        // Given measurement publisher
        var sut = new MeasurementSyncPublisher(_mapper, _logger, _publishEndpoint);

        // When publishing events
        var gsrn = Any.Gsrn();
        var meteringPoint = Any.MeteringPoint(gsrn);
        var meteringPointOwner = Guid.NewGuid().ToString();
        var syncInfo = new MeteringPointSyncInfo(gsrn, DateTimeOffset.UtcNow, meteringPointOwner, MeteringPointType.Production, "DK1", Guid.NewGuid(),
            Any.Technology());
        var measurement1 = Any.Measurement(gsrn, DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(), 10);
        var measurement2 = Any.Measurement(gsrn, DateTimeOffset.UtcNow.AddHours(-5).ToUnixTimeSeconds(), 10);
        await sut.PublishIntegrationEvents(meteringPoint, syncInfo, [measurement1, measurement2], CancellationToken.None);

        // Events are published to bus
        await _publishEndpoint.Received(2).Publish(Arg.Any<EnergyMeasuredIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
