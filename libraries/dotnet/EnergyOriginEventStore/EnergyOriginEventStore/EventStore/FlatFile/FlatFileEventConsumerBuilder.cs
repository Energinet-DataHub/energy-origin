using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.FlatFile;

internal class FlatFileEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers = new();

    private readonly FlatFileEventStore _fileStore;
    private readonly string _topicPrefix;
    private string? _pointer;
    private Action<string, Exception>? _exceptionHandler;

    public FlatFileEventConsumerBuilder(FlatFileEventStore fileStore, string topicPrefix)
    {
        _fileStore = fileStore;
        _topicPrefix = topicPrefix;
    }

    public IEventConsumer Build()
    {
        var unpacker = new Unpacker();
        var consumer = new FlatFileEventConsumer(unpacker, _handlers, _exceptionHandler, _fileStore, _topicPrefix, _pointer);

        _fileStore.DisposeEvent += consumer.Dispose;

        return consumer;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        var type = typeof(T);

        var list = _handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        Action<Event<EventModel>> casted_handler = e => handler(new Event<T>((T)e.EventModel, e.Pointer));
        _handlers[type] = list.Append(casted_handler);

        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        _pointer = pointer;
        return this;
    }

    public IEventConsumerBuilder SetExceptionHandler(Action<string, Exception> handler)
    {
        _exceptionHandler = handler;
        return this;
    }
}
