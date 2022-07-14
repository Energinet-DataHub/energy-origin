using System;
using EventStore.Serialization;

namespace EventStore;

public interface IEventStore<T> where T : EventModel {
    void Produce(T model, IEnumerable<string> topics);

    IEventConsumer<T> MakeConsumer(string topicPrefix);
    IEventConsumer<T> MakeConsumer(string topicPrefix, DateTime fromDate);

    // void Delete(string topic);
    // sample topics:
    // - user-1234567890-sensitive
    // - user-1234567890-data
    // - all-users
}
