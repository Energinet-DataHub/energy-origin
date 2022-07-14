using EventStore.Serialization;

namespace EventStore;

public interface IEventStore
{
    Task Produce(EventModel model, IEnumerable<string> topics);
    IEventConsumer<T> MakeConsumer<T>(string topicPrefix) where T : EventModel;
    IEventConsumer<T> MakeConsumer<T>(string topicPrefix, DateTime fromDate) where T : EventModel;
}
