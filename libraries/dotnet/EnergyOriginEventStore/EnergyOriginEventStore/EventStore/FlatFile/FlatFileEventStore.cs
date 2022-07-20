using Newtonsoft.Json;
using EventStore.Serialization;

namespace EventStore.FlatFile;

public class FlatFileEventStore : IEventStore
{
    public string ROOT => "store";
    public string TOPIC_SUFFIX => ".topic";
    public string EVENT_SUFFIX => ".event";

    public FlatFileEventStore()
    {
        if (!Directory.Exists(ROOT))
        {
            Directory.CreateDirectory(ROOT);
        }
    }

    public async Task Produce(EventModel model, params string[] topics)
    {
        var message = InternalEvent.From(model);

        foreach (var topic in topics)
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
        return new FlatFileEventConsumerBuilder(this, topicPrefix);
    }

    public event Action? DisposeEvent;

    public void Dispose()
    {
        DisposeEvent?.Invoke();
    }
}
