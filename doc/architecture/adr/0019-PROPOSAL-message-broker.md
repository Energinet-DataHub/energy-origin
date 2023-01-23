Message broker

* Status: Proposed
* Deciders: @anna-knudsen, @duizer, @ckr123, @Exobitt
* Date: 2022-01-19

---

## Context and Problem Statement

For the certificates domain, we need a message broker for our event driven architecture solution.
We need a message broker that can handle a large number of messages and is fairly easy to set up.

---

## Considered Options

* Kafka
* RabbitMQ
* Redis
* ActiveMQ

---

## Decision Outcome

We are going with RabbitMQ

## Rationale

### RabbitMQ:
RabbitMQ seems to be a simple message broker and not much else, which is exactly what we need.
It is a very popular, open source message broker with lots of support and documentation.
It also seems very easy to set up.

### Kafka:
We chose not to go with Kafka, as it seems very complex - it is more like a streaming service and provides much more functionality than we need.
We get the impression that Kafka requires experts to fine tune after the initial setup

### ActiveMQ:
Active MQ is mostly for Java.

### Redis:
Redis doesn't have built-in SSL and offers no persistence out of the box.

### Positive Consequences
* Easy setup
* Lots of documentation

### Negative Consequences

We are not yet 100% sure that RabbitMQ can handle the number of messages we need, and we are aware that RabbitMQ can not be scaled to the extent that Kafka can.
We chose to go with RabbitMQ anyway, as we want to create a quick POC. If needed, we will substitute RabbitMQ for another, more scalable message broker later.
