using Newtonsoft.Json;
using EventStore.Serialization;

namespace EventStore.FlatFile;

public class FlatFileEventStore : IEventStore
{

    const string ROOT = "store";
    const string TOPIC_SUFFIX = ".topic";
    const string EVENT_SUFFIX = ".event";

    public FlatFileEventStore()
    {
        if (!Directory.Exists(ROOT))
        {
            Directory.CreateDirectory(ROOT);
        }
    }

    public async Task Produce(EventModel model, IEnumerable<string> topics)
    {
        var message = InternalEvent.From(model);

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


    public IEventConsumerBuilder GetBuilder(string topicPrefix)
    {
        return new FlatFileEventConsumerBuilder(ROOT, TOPIC_SUFFIX, EVENT_SUFFIX, topicPrefix);
    }
}
