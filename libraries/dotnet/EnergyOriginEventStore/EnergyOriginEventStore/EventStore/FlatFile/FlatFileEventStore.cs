using System;
using System.IO;
using Newtonsoft.Json;
using EventStore;
using EventStore.Serialization;

namespace EventStore.Flatfile;

public class FlatFileEventStore<T> : IEventStore<T> where T : EventModel
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

    public void Produce(T model, IEnumerable<string> topics)
    {
        var message = Event.From(model);

        foreach (string topic in topics)
        {
            var path = $"{ROOT}/{topic}{TOPIC_SUFFIX}/"; // should have more directory division
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText($"{path}/{message.Issued}-{message.Id}{EVENT_SUFFIX}", JsonConvert.SerializeObject(message));
        }
    }

    public IEventConsumer<T> MakeConsumer(string topicPrefix) => CreateConsumer(topicPrefix, null);
    public IEventConsumer<T> MakeConsumer(string topicPrefix, DateTime fromDate) => CreateConsumer(topicPrefix, fromDate);

    IEventConsumer<T> CreateConsumer(string topicPrefix, DateTime? fromDate) => new FlatFileEventConsumer<T>(unpacker, ROOT, TOPIC_SUFFIX, EVENT_SUFFIX, topicPrefix, fromDate);
}
