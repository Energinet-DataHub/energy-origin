using EnergyOriginEventStore.EventStore.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Database;

public class DatabaseEventStore : IEventStore
{
    private DatabaseEventContext context;

    public DatabaseEventStore(string connectionString)
    {
        context = new DatabaseEventContext(connectionString);
    }

    #region IEventStore

    public async Task Produce(EventModel model, params string[] topics)
    {
        var message = InternalEvent.From(model);

        var json = JsonConvert.SerializeObject(message);

        foreach (var topic in topics)
        {
            context.Messages.Add(new Message() { Topic = topic, Payload = json });
        }
        await context.SaveChangesAsync();
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new DatabaseEventConsumerBuilder(context, topicPrefix);

    public void Dispose() { }

    #endregion
}
