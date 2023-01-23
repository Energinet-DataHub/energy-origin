# Certificates Domain

The diagrams below are divided into:

* [Current architecture:](#current-architecture) Diagrams for what Team Atlas are building in the current or up-coming sprint
* [Target architecture:](#target-architecture) Diagrams for the desired target

The reason for this split is certain constraints. The constraints are:

* Registry is under development
* Integration Event Bus does not exist and the inter-domain events are not defined

## Current architecture

### Container diagram
![Container diagram](../diagrams/certificates.current.container.drawio.svg)

## Component diagram: Certificate API

The component diagram shows a first iteration which is based on an in-memory integration event bus. A consequence of using the in-memory implementation is that all components that is dependent on the integration event bus must be in same container.

Components that is used for mocking and will be replaced or discarded at a later are marked with its own color in the diagram.

Note: `ContractService` is currently getting information about a metering point from `DataSync`. In the future it is expected to get this from the MeteringPoints domain, but this domain does not exist at this point.

![Issuer component diagram](../diagrams/certificates.current.component.certificate.api.drawio.svg)

## Target architecture

### Container diagram
![Container diagram](../diagrams/certificates.target.container.drawio.svg)

