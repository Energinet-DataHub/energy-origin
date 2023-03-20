# Certificates Domain
This is the certificates domain.

## Key generation

An Ed25519 key must be generated for the issuing body of certificates. Project Origin Registry requires that the signature algorithm is Ed25519. To be consistent with the Auth domain, the format used is PEM. The following generates a key and does base64 encoding:

```
openssl genpkey -algorithm ed25519 | base64 -w 0
```

A key is generated and added as a sealed secret in eo-base-environment. This is the private key. There will be created a key per environment type (preview, test and production).

Project Origin Registry must know the public key for the issuer for the relevant grid areas (e.g. "DK1" and "DK2") and the format here must be "RawPublicKey" (see https://nsec.rocks/docs/api/nsec.cryptography.keyblobformat).

## For local development
In order to test and develop locally, enter the docker-test-env and run:
```
docker-compose up
```
When shutting down, run:
```
docker-compose down --volumes
```

## Domain decisions / DDR (Domain-decision-records)

* Use MassTransit to have an abstraction on top of message transports such as RabbitMQ. It can also use Marten for saga persistence. For now it is used as in-memory bus for integration events.
* Use MartenDB for event store.
* FluentValidation to make assertions in test more readable. Furthermore, FluentAssertions has support for "BeEquivalentTo", see https://fluentassertions.com/objectgraphs/.
