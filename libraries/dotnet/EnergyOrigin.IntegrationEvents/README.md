# EnergyOrigin.IntegrationEvents

## Overview

The EnergyOrigin.IntegrationEvents NuGet package provides a standardized way to define and use integration events,
across the Energy Origin system. It facilitates communication between different microservices and components,
through a message-driven architecture.

## How it works

The package contains an abstract IntegrationEvent record that serves as the base for all integration events.
This base record includes common properties such as:

- **Id:** A unique identifier for the event.
- **TraceId:** An identifier for tracing the event through the system.
- **Created:** The timestamp when the event was created.

**IntegrationEvent.cs:**

```csharp
public abstract record IntegrationEvent
{
protected IntegrationEvent(Guid id, string traceId, DateTimeOffset created)
{
Id = id;
TraceId = traceId;
Created = created;
}

    public Guid Id { get; }
    public string TraceId { get; }
    public DateTimeOffset Created { get; }
}
```

### Integration Events

Specific integration events are defined as records, that inherit from IntegrationEvent,
adding their own properties, as needed.

- [Authorization: Acceptance of Terms](./doc/specific-events/authorization-acceptance-of-terms.md)

### Integration guides for various message brokers:

- [RabbitMQ using MassTransit's Transactional Outbox Pattern](./doc/specific-integration-guides/masstransit-rabbitmq.md)
