using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore;

public interface IEventStore : IDisposable
{
    public Task Produce(EventModel model, params string[] topics);

    public IEventConsumerBuilder GetBuilder(string topicPrefix);
}
