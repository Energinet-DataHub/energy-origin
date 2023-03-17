Message broker certificates domain

* Status: Accepted
* Deciders: @anna-knudsen, @duizer, @ckr123, @Exobitt
* Date: 2023-01-19

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

A quick calculation on the number of messages going through the message broker, could be based on a case where every measurement from a metering point in DK1+DK2 is a message resulting in an issued certificate. With 3,500,000 metering points in DK1+DK2 and with a measurement every hour that will be approximately 1,000 messages/second. If the market resolution changes to 15 minutes, then the number is 4,000 messages/second.

We chose to go with RabbitMQ, as we want to create a quick POC and articles mentions that RabbitMQ can handle around 50,000 messages/second. If needed, we will substitute RabbitMQ for another, more scalable message broker later.
