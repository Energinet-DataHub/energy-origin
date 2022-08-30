using EnergyOriginEventStore.EventStore.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Database;

public class DatabaseEventStore : IEventStore
{
    private readonly DatabaseEventContext context;

    public DatabaseEventStore(DatabaseEventContext context) => this.context = context;

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

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new DatabaseEventConsumerBuilder(context.Clone, topicPrefix);

    public void Dispose() => GC.SuppressFinalize(this);

    #endregion
}
