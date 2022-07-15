using EventStore.Serialization;

namespace EventStore;

public interface IEventConsumerBuilder
{
    IEventConsumer Build();

    IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel;

    IEventConsumerBuilder ContinueFrom(string pointer);
}
