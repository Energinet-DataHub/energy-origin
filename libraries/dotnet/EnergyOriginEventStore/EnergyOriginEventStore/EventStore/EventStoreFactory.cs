using System;
using EventStore.Serialization;
using EventStore.Flatfile;

namespace EventStore;

public class EventStoreFactory<T> where T : EventModel
{
    public static IEventStore<T> create()
    {
        return new FlatFileEventStore<T>();
    }
}
