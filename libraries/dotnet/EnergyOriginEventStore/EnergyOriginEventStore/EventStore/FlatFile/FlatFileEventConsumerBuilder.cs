using EventStore.Internal;
using EventStore.Serialization;

namespace EventStore.FlatFile;

internal class FlatFileEventConsumerBuilder : IEventConsumerBuilder
{

    private Dictionary<Type, IEnumerable<Action<EventModel>>> handlers = new Dictionary<Type, IEnumerable<Action<EventModel>>>();

    private string ROOT;
    private string TOPIC_SUFFIX;
    private string EVENT_SUFFIX;
    private string topicPrefix;

    public FlatFileEventConsumerBuilder(string ROOT, string TOPIC_SUFFIX, string EVENT_SUFFIX, string topicPrefix)
    {
        this.ROOT = ROOT;
        this.TOPIC_SUFFIX = TOPIC_SUFFIX;
        this.EVENT_SUFFIX = EVENT_SUFFIX;
        this.topicPrefix = topicPrefix;
    }

    public IEventConsumer Build()
    {
        var unpacker = new Unpacker();
        return new FlatFileEventConsumer(unpacker, handlers, ROOT, TOPIC_SUFFIX, EVENT_SUFFIX, topicPrefix, null);
    }

    public IEventConsumerBuilder AddHandler<T>(Action<T> handler) where T : EventModel
    {
        Type t = typeof(T);

        var list = handlers.GetValueOrDefault(t);
        if (list is null)
        {
            list = new List<Action<EventModel>>();
        }

        Action<EventModel> casted_handler = (e) => handler((T)e);
        handlers[t] = list.Append(casted_handler);

        return this;
    }
}
