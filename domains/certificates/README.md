# Certificates Domain
This is the certificates domain.


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
