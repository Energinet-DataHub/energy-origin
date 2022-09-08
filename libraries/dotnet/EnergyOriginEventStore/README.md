# event-store

## Options

### Redis
---
Disqualified since all data must be held in memory.

https://redis.io/docs/about/

> To achieve top performance, Redis works with an in-memory dataset. Depending on your use case, Redis can persist your data either by periodically dumping the dataset to disk or by appending each command to a disk-based log. You can also disable persistence if you just need a feature-rich, networked, in-memory cache.


### EventStoreDB
---
All [case studies](https://www.eventstore.com/case-studies/) are small customers vs the customers listed in [Who is using Event Store?](https://www.eventstore.com/).

Newst case study is from 2018.

A [case study](https://www.eventstore.com/case-studies/parcelvision) from 2015 mentions through-put of 50k/h.

> Parcelvision can provide up to 50,000 quotations per hour, with each quotation generating multiple events. EventStoreDB easily handles all the events and quotations, freeing up their developers to focus on the business needs rather than the onerous maintenance of the database.



No operator is available at present, source [EventStore.Charts](https://github.com/EventStore/EventStore.Charts).

> As such we will be devoting resources to develop a Kubernetes operator that satisfies these requirements, for release at a future date.

We have anecdotal evidence for poor performance.

### Kafka
---

[Kafka](https://kafka.apache.org/):
- is battletested
- is open source
- has a choice of operators
- has great testimonals

Sample testimonal:
> Kafka is powering our high-flow event pipeline that aggregates over 1.2 billion metric series from 1000+ data centers for near-to-real time data center operational analytics and modeling

We have anecdotal evidence for great performance.

*but*

Kafka is a bit of a beast to operate/use. The featureset is much greater than our needs.

### "YAES"
---

Rolling our own implementation (yet another event store)

## Discussion

We need to proceed with something, so a proof of concept have been put together (poorly). So that we may discuss the interface and message structure.

### Message JSON
---

```
# Example of en-/de-coding event messages:

- Sending message of type 'Said':
{"Actor":"Anton Actor","Statement":"I like to act!"}

- Packaged:
{"Id":"5192a593-8cc1-464e-81af-cd55709dc8d1","Issued":1657206578,"IssuedFraction":493,"ModelType":"Said","ModelVersion":1,"Data":"{\"Actor\":\"Anton Actor\",\"Statement\":\"I like to act!\"}"}

- Received message of type 'Said':
{"Actor":"Anton Actor","Statement":"I like to act!"}
```

### Interface
---

```C#
public interface IEventStore : IDisposable
{
    Task Produce(EventModel model, params string[] topics);

    IEventConsumerBuilder GetBuilder(string topicPrefix);
}
```

### FlatFile POC
---

Potentially useful on:
- a single instance mounted a single disk
- multiple instances mounting a shared disk
- instances mounting a ceph/rook

Note: The POC has been removed.

## Misc

### Outstanding Questions
---

- how to configure storage within azure? Not sure, but this seems important,

```
kind: StorageClass -> allowVolumeExpansion: true
```

- What is the maximum file count and maximum storage capacity when using ceph/rook?

### Links
---

- https://github.com/confluentinc/confluent-kafka-dotnet | Library/Client for Kafka from Confluent
- https://developer.confluent.io/get-started/dotnet/ | A guide to Library/Client for Kafka from Confluent
- https://portworx.com/blog/choosing-the-right-kubernetes-operator-for-apache-kafka/ | Kafka Operator Choice
- https://blog.devgenius.io/apache-kafka-on-kubernetes-using-strimzi-27d47b6b13bc | Strimzi usage
