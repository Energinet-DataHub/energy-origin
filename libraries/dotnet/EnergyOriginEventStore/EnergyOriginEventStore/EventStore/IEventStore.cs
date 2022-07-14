using System;
using EventStore.Serialization;

namespace EventStore;

public interface IEventStore
{
    void Produce(EventModel model, IEnumerable<string> topics);

    IEventConsumer<T> MakeConsumer<T>(string topicPrefix) where T : EventModel;
    IEventConsumer<T> MakeConsumer<T>(string topicPrefix, DateTime fromDate) where T : EventModel;

    // void Delete(string topic);
    // sample topics:
    // - user-1234567890-sensitive
    // - user-1234567890-data
    // - all-users
}
