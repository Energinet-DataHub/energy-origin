using EventStore.Internal;
using EventStore.Serialization;

namespace EventStore.FlatFile;

internal class FlatFileEventConsumerBuilder : IEventConsumerBuilder
{

    private Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers = new Dictionary<Type, IEnumerable<Action<Event<EventModel>>>>();

    private FlatFileEventStore fileStore;
    private string topicPrefix;
    private string? pointer = null;

    public FlatFileEventConsumerBuilder(FlatFileEventStore fileStore, string topicPrefix)
    {
        this.fileStore = fileStore;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumer Build()
    {
        var unpacker = new Unpacker();
        var consumer = new FlatFileEventConsumer(unpacker, handlers, fileStore, topicPrefix, pointer);

        fileStore.disposeEvent += consumer.Dispose;

        return consumer;
    }

    public IEventConsumerBuilder AddHandler<T>(Action<Event<T>> handler) where T : EventModel
    {
        Type t = typeof(T);

        var list = handlers.GetValueOrDefault(t);
        if (list is null)
        {
            list = new List<Action<Event<EventModel>>>();
        }

        Action<Event<EventModel>> casted_handler = (e) => handler(new Event<T>((T)e.EventModel, e.Pointer));
        handlers[t] = list.Append(casted_handler);

        return this;
    }

    public IEventConsumerBuilder ContinueFrom(string pointer)
    {
        this.pointer = pointer;
        return this;
    }
}
