using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore;

public interface IEventConsumerBuilder
{
    IEventConsumer Build();

    IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel;

    IEventConsumerBuilder ContinueFrom(string pointer);
}
