using System;
using System.IO;
using Newtonsoft.Json;
using EventStore;
using EventStore.Serialization;

namespace EventStore.Flatfile;

public class FlatFileEventStore<T> : IEventStore<T> where T : EventModel {
    const string ROOT = "topics";

    public void Produce(T model, IEnumerable<string> topics) {
        var message = Event.From(model);

        foreach(string topic in topics) {
            var path = $"{ROOT}/{message.ModelType}-{topic}/"; // should have more directory division
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText($"{path}/{message.Issued}-{message.Id}", JsonConvert.SerializeObject(message));
        }
    }

    public IEventConsumer<T> MakeConsumer(string topicPrefix) => CreateConsumer(topicPrefix, null);
    public IEventConsumer<T> MakeConsumer(string topicPrefix, DateTime fromDate) => CreateConsumer(topicPrefix, fromDate);

    IEventConsumer<T> CreateConsumer(string topicPrefix, DateTime? fromDate) => new FlatFileEventConsumer<T>(ROOT, topicPrefix, fromDate);
}
