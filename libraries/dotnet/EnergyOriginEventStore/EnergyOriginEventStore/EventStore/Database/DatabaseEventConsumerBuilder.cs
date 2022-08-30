using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Database;

internal class DatabaseEventConsumerBuilder : IEventConsumerBuilder
{
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers = new();
    private Action<string, Exception>? exceptionHandler;
    private readonly DatabaseEventContext context;
    private readonly string topicPrefix;
    private string? pointer;

    public DatabaseEventConsumerBuilder(DatabaseEventContext context, string topicPrefix)
    {
        this.context = context;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        var type = typeof(T);

        var list = handlers.GetValueOrDefault(type) ?? new List<Action<Event<EventModel>>>();

        void castedHandler(Event<EventModel> e) => handler(new Event<T>((T)e.EventModel, e.Pointer));
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
        this.pointer = pointer;
        return this;
    }

    public IEventConsumer Build() => new DatabaseEventConsumer(new Unpacker(), handlers, exceptionHandler, context, topicPrefix, pointer);
}
