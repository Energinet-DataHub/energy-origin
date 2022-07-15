using EventStore.Serialization;

namespace EventStore;

public interface IEventConsumerBuilder
{
    IEventConsumer Build();

    IEventConsumerBuilder AddHandler<T>(Action<T> handler) where T : EventModel;
}
