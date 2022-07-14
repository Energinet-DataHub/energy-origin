using Newtonsoft.Json;
using EventStore.Serialization;

namespace EventStore.Flatfile;

public class FlatFileEventStore : IEventStore
{

    const string ROOT = "store";
    const string TOPIC_SUFFIX = ".topic";
    const string EVENT_SUFFIX = ".event";
    private Unpacker unpacker;

    public FlatFileEventStore()
    {
        if (!Directory.Exists(ROOT))
        {
            Directory.CreateDirectory(ROOT);
        }

        this.unpacker = new Unpacker();
    }

    public async Task Produce(EventModel model, IEnumerable<string> topics)
    {
        var message = Event.From(model);

        foreach (string topic in topics)
        {
            var path = $"{ROOT}/{topic}{TOPIC_SUFFIX}/"; // should have more directory division
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            await File.WriteAllTextAsync($"{path}/{message.Issued}-{message.Id}{EVENT_SUFFIX}", JsonConvert.SerializeObject(message));
        }
    }

    public IEventConsumer<T> MakeConsumer<T>(string topicPrefix) where T : EventModel => CreateConsumer<T>(topicPrefix, null);
    public IEventConsumer<T> MakeConsumer<T>(string topicPrefix, DateTime fromDate) where T : EventModel => CreateConsumer<T>(topicPrefix, fromDate);

    IEventConsumer<T> CreateConsumer<T>(string topicPrefix, DateTime? fromDate) where T : EventModel => new FlatFileEventConsumer<T>(unpacker, ROOT, TOPIC_SUFFIX, EVENT_SUFFIX, topicPrefix, fromDate);
}
