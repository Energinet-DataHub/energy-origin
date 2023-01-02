# Certificates Domain (New design)

## Working assumptions

The following lists the working assumptions. The design/architecture used needs to address the working assumptions; hence, the working assumptions are treated as facts. In the case a working assumption is wrong, it will be listed here stating the assumption and why it is incorrect

### Project Origin Registry

* Should be treated as an external system meaning that it may not be available
* Client library is designed to be used asynchronously
* Should not be used as a database for certificates
* It is not possible to query the registry, only commands (e.g. issue certificate, slice or transfer slice) can be sent to the registry
  * Observation point from XPTDU: We will most likely have "double book-keeping" of the certificates on top of the Project Origin Registry. It is possible to we end up with mis-aligned certificates; e.g. a certificate in Project Origin Registry has 2 slices, but in EnergyOrigin it has 3 slices. In case a certificate gets misaligned, we need to do some compensating action. A way of doing that automatically could be to treat Project Origin Registry as the truth and then read the state or events for that certifacte from Project Origin Registry and then apply that to the certifate in Energy Origin. However, this compensating action is not possible if queries are not implemented by Project Origin Registry
* Commands is expected to take relatively long time. When a federated setup of registries is available, commands spanning multi registries will take even longer time (we are talking seconds here, not microseconds)
* Is not idempotent, so we should ensure that a command is not sent twice
  * Note from XPTDU: Not really sure about this. What will the registry do if the same command is sent multiple times?
* No locking mechanism for commands against a certificate exists. So if two commands are sent at the same time, only one is expected to win.
  * Observation point from XPTDU: If a certificate needs to be locked, then it must be done in Energy Origin (or by the Energy Origin API client)
