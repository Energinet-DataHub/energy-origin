using EnergyOriginEventStore.EventStore.Serialization;
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
            Topic dbTopic;
            try
            {
                dbTopic = context.Topics.Where(it => it.Name == topic).Single();
            }
            catch
            {
                dbTopic = new Topic() { Name = topic };
                context.Topics.Add(dbTopic);
                await context.SaveChangesAsync();
            }

            context.Messages.Add(new Message() { Topic = dbTopic, Payload = json });
        }
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new DatabaseEventConsumerBuilder(this, topicPrefix);

    public void Dispose() { }

    #endregion
}
