using System;

namespace EventStore;

// FIXME: will not compile at all

public interface IEventStore<T> where T : EventModel {
    void Produce(T model, IEnumerable<string> topics);
    EventConsumer<T> MakeConsumer(string topicPrefix, DateTime? fromDate);
}

public interface EventConsumer<T> { // can contain re-ordering mechanisms
    T Consume();
}