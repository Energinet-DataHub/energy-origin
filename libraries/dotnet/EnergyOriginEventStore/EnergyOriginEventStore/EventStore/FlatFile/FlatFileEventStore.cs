using EnergyOriginEventStore.EventStore.Serialization;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.FlatFile;

public class FlatFileEventStore : IEventStore
{
    public static string ROOT => "store";
    public static string TOPIC_SUFFIX => ".topic";
    public static string EVENT_SUFFIX => ".event";

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

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new FlatFileEventConsumerBuilder(this, topicPrefix);

    public event Action? DisposeEvent;

    public void Dispose()
    {
        DisposeEvent?.Invoke();
        GC.SuppressFinalize(this);
    }
}
