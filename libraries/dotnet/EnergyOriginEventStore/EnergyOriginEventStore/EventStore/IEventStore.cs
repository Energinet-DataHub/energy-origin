using EventStore.Serialization;

namespace EventStore;

public interface IEventStore : IDisposable
{
    Task Produce(EventModel model, params string[] topics);

    IEventConsumerBuilder GetBuilder(string topicPrefix);
}
