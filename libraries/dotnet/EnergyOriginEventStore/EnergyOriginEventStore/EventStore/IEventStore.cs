using EventStore.Serialization;

namespace EventStore;

public interface IEventStore
{
    Task Produce(EventModel model, params string[] topics);

    IEventConsumerBuilder GetBuilder(string topicPrefix);
}
