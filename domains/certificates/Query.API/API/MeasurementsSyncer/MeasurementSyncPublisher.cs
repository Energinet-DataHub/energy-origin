using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using MassTransit;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;

namespace API.MeasurementsSyncer;

public interface IMeasurementSyncPublisher
{
    Task PublishIntegrationEvents(MeteringPoint meteringPoint, MeteringPointSyncInfo syncInfo, List<Measurement> measurements, CancellationToken cancellationToken);
}

public class MeasurementSyncPublisher : IMeasurementSyncPublisher
{
    private readonly EnergyMeasuredIntegrationEventMapper _mapper;
    private readonly ILogger<MeasurementSyncPublisher> _logger;
    private readonly IPublishEndpoint _bus;

    public MeasurementSyncPublisher(EnergyMeasuredIntegrationEventMapper mapper, ILogger<MeasurementSyncPublisher> logger, IPublishEndpoint bus)
    {
        _mapper = mapper;
        _logger = logger;
        _bus = bus;
    }

    public async Task PublishIntegrationEvents(MeteringPoint meteringPoint, MeteringPointSyncInfo syncInfo, List<Measurement> measurements,
        CancellationToken cancellationToken)
    {
        var integrationsEvents = _mapper.MapToIntegrationEvents(meteringPoint, syncInfo, measurements);
        _logger.LogInformation("Publishing {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count);

        foreach (var @event in integrationsEvents)
        {
            await _bus.Publish(@event, cancellationToken);
        }

        _logger.LogInformation("Published {numberOfEnergyMeasuredIntegrationEvents} energyMeasuredIntegrationEvents to the Integration Bus",
            integrationsEvents.Count);
    }
}
