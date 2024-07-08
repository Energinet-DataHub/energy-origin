# EnergyOrigin.IntegrationEvents

## Overview

The EnergyOrigin.IntegrationEvents NuGet package provides our way to define and use integration events,
across the Energy Origin system. It facilitates communication between different microservices and components,
through a message-driven architecture.

## How it works

- [Integration Events](../../../doc/architecture/adr/0025-integration-events.md)

### Specific Integration Events

Specific integration events are defined as records, that inherit from IntegrationEvent,
adding their own properties, as needed.

- [Authorization: Acceptance of Terms](./doc/specific-events/authorization-acceptance-of-terms.md)

### Integration guides for various message brokers:

- [RabbitMQ using MassTransit's Transactional Outbox Pattern](./doc/specific-integration-guides/masstransit-rabbitmq.md)
