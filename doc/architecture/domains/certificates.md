# Certificates Domain

The diagrams below are divided into:

* [Current architecture:](#current-architecture) Diagrams for what Team Atlas are building in the current or up-coming sprint
* [Target architecture:](#target-architecture) Diagrams for the desired target

The reason for this split is certain constraints. The constraints are:

* Registry is under development
* Integration Event Bus does not exist and the inter-domain events are not defined

## Current architecture (as-is)

### Container diagram
![Container diagram](../diagrams/certificates.current.container.drawio.svg)

### Message flow
![Message flow](../diagrams/certificates.current.messageflow.drawio.svg)

## Component diagram: Certificate API

The component diagram shows how the solution works, based on a RabbitMQ message broker that publishes the events received from the DataSyncSyncer.

Note: `ContractService` is currently getting information about a metering point from `DataSync`. In the future it is expected to get this from the MeteringPoints domain, but this domain does not exist at this point.

![Issuer component diagram](../diagrams/certificates.current.component.certificate.api.drawio.svg)

## Target architecture (to-be)

### Container diagram
![Container diagram](../diagrams/certificates.target.container.drawio.svg)

