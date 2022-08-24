using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.FlatFile;

internal class FlatFileEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers = new();

    private readonly FlatFileEventStore fileStore;
    private readonly string topicPrefix;
    private string? pointer;
    private Action<string, Exception>? exceptionHandler;

    public FlatFileEventConsumerBuilder(FlatFileEventStore fileStore, string topicPrefix)
    {
        this.fileStore = fileStore;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumer Build()
    {
        var unpacker = new Unpacker();
        var consumer = new FlatFileEventConsumer(unpacker, handlers, exceptionHandler, fileStore, topicPrefix, pointer);

        fileStore.DisposeEvent += consumer.Dispose;

        return consumer;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        var type = typeof(T);

        var list = handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        Action<Event<EventModel>> casted_handler = e => handler(new Event<T>((T)e.EventModel, e.Pointer));
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
