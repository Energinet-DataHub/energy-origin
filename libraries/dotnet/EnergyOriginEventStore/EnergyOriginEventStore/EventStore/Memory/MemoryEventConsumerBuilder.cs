using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers = new();
    private Action<string, Exception>? exceptionHandler;
    private readonly MemoryEventStore store;
    private readonly string topicPrefix;
    private MemoryPointer? pointer;

    public MemoryEventConsumerBuilder(MemoryEventStore store, string topicPrefix)
    {
        this.store = store;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        var type = typeof(T);

        var list = handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        Action<Event<EventModel>> castedHandler = e => handler(new Event<T>((T)e.EventModel, e.Pointer));
        handlers[type] = list.Append(castedHandler);

        return this;
    }

    public IEventConsumerBuilder SetExceptionHandler(Action<string, Exception> handler)
    {
        exceptionHandler = handler;
        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        this.pointer = new MemoryPointer(pointer);
        return this;
    }

    public IEventConsumer Build() => new MemoryEventConsumer(new Unpacker(), handlers, exceptionHandler, store, topicPrefix, pointer);
}
