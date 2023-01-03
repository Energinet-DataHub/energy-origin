# Certificates Domain (New design)

## Working assumptions

The following lists the working assumptions. The design/architecture used needs to address the working assumptions; hence, the working assumptions are treated as facts. In the case a working assumption is wrong, it will be listed here stating the assumption and why it is incorrect

### Project Origin Registry

* Should be treated as an external system meaning that it may not be available
* Client library is designed to be used asynchronously
* Should not be used as a database for certificates
* It is not possible to query the registry, only commands (e.g. issue certificate, slice or transfer slice) can be sent to the registry
  * Observation point from XPTDU: We will most likely have "double bookkeeping" of the certificates on top of the Project Origin Registry. It is possible to we end up with misaligned certificates; e.g. a certificate in Project Origin Registry has 2 slices, but in EnergyOrigin it has 3 slices. In case a certificate gets misaligned, we need to do some compensating action. A way of doing that automatically could be to treat Project Origin Registry as the truth and then read the state or events for that certifacte from Project Origin Registry and then apply that to the certifate in Energy Origin. However, this compensating action is not possible if queries are not implemented by Project Origin Registry
* Commands is expected to take relatively long time. When a federated setup of registries is available, commands spanning multi registries will take even longer time (we are talking seconds here, not microseconds)
* Is not idempotent, so we should ensure that a command is not sent twice
  * Note from XPTDU: Not really sure about this. What will the registry do if the same command is sent multiple times?
* No locking mechanism for commands against a certificate exists. So if two commands are sent at the same time, only one is expected to win.
  * Observation point from XPTDU: If a certificate needs to be locked, then it must be done in Energy Origin (or by the Energy Origin API client)
  * It is not possible to do a distributed transaction

## Relationship between Project Origin Registry and data within certificates domain

* There will be double bookkeeping of certificates in our domain and Project Origin Registry
* Misalignment between the systems is possible and should be handled somehow
* For now, the approach taken is that Project Origin Registry is considered the truth. This mean that commands like Transfer, Claim and Slice will follow this recipe
  * First run the command against registry
  * If command was registry successfully applied, then apply the same to Certificates Domain model

## Synchronous approach

TODO...

## Event driven approach - Choreography

For difference between choreography and orchestration see https://codeopinion.com/event-choreography-orchestration-sagas/

Below is an example of how we could do event driven communication based on choreography for a transfer slice command. A box should be seen as something that consumes and/or sends an event to the message broker.

![Events for transfer slice - choreography](../diagrams/certificates.events.choreography.drawio.svg)

The bottom part shows the flow of events.

"Validator" could be left out or be a part of Command API. "Validator" can do some validation of the request/command before RegistryConnector sends a command to Project Origin Registry. The validation could e.g. be a check if the slice exists or if the calling user has the needed rights.

## Event driven approach - Orchestration

For difference between choreography and orchestration see https://codeopinion.com/event-choreography-orchestration-sagas/

TODO...
