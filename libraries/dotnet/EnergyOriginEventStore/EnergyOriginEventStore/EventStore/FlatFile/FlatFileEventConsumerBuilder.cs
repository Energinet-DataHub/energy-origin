using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.FlatFile;

internal class FlatFileEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers = new();

    private readonly FlatFileEventStore store;
    private readonly string topicPrefix;
    private string? pointer;
    private Action<string, Exception>? exceptionHandler;

    public FlatFileEventConsumerBuilder(FlatFileEventStore store, string topicPrefix)
    {
        this.store = store;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumer Build()
    {
        var unpacker = new Unpacker();
        var consumer = new FlatFileEventConsumer(unpacker, handlers, exceptionHandler, topicPrefix, pointer);

        store.DisposeEvent += consumer.Dispose;

        return consumer;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        var type = typeof(T);

        var list = handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        void casted_handler(Event<EventModel> e) => handler(new Event<T>((T)e.EventModel, e.Pointer));
        handlers[type] = list.Append(casted_handler);

        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        this.pointer = pointer;
        return this;
    }

    public IEventConsumerBuilder SetExceptionHandler(Action<string, Exception> handler)
    {
        exceptionHandler = handler;
        return this;
    }
}
