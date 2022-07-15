using EventStore.Serialization;

namespace EventStore;

public interface IEventStore
{
    Task Produce(EventModel model, IEnumerable<string> topics);

    IEventConsumerBuilder GetBuilder(string topicPrefix);
}
