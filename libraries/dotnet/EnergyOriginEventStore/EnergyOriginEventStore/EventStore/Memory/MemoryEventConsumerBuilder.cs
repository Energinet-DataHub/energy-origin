using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers = new();

    private readonly MemoryEventStore _store;
    private readonly string _topicPrefix;
    private string? _pointer;

    public MemoryEventConsumerBuilder(MemoryEventStore store, string topicPrefix)
    {
        _store = store;
        _topicPrefix = topicPrefix;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        Type type = typeof(T);

        var list = _handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        Action<Event<EventModel>> castedHandler = e => handler(new Event<T>((T)e.EventModel, e.Pointer));
        _handlers[type] = list.Append(castedHandler);

        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        _pointer = pointer;
        return this;
    }

    public IEventConsumer Build()
    {
        return new MemoryEventConsumer(new Unpacker(), _handlers, _store, _topicPrefix, _pointer);
    }
}
