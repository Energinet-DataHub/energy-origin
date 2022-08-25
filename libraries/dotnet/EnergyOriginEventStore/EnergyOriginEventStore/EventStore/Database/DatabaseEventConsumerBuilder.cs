using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Database;

internal class DatabaseEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers = new();
    private Action<string, Exception>? _exceptionHandler;
    private readonly DatabaseEventContext _context;
    private readonly string _topicPrefix;
    private string? _pointer;

    public DatabaseEventConsumerBuilder(DatabaseEventContext store, string topicPrefix)
    {
        _context = store;
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

    public IEventConsumerBuilder SetExceptionHandler(Action<string, Exception> handler)
    {
        _exceptionHandler = handler;
        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        _pointer = pointer;
        return this;
    }

    public IEventConsumer Build() => new DatabaseEventConsumer(new Unpacker(), _handlers, _exceptionHandler, _context, _topicPrefix, _pointer);
}
