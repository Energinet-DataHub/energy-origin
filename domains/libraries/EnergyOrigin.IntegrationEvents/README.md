# EnergyOrigin.IntegrationEvents

## Overview

The EnergyOrigin.IntegrationEvents NuGet package provides our way of sharing integration event contracts,
between our systems, across the Energy Track & Trace™.
It facilitates communication between different microservices and components, through a message-driven architecture.

## How it works

- [Integration Events](../../../doc/architecture/adr/0025-integration-events.md)

### Specific Integration Events

Specific integration events are defined as records, that inherit from IntegrationEvent,
adding their own properties, as needed.

- [Authorization: Acceptance of Terms](../../../domains/libraries/EnergyOrigin.IntegrationEvents/EnergyOrigin.IntegrationEvents/Events/Terms/authorization-acceptance-of-terms.md)
