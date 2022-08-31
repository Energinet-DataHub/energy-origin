namespace EnergyOriginEventStore.EventStore.Database;

public interface DatabaseEventContext
{
    Task Add(Message message);
    Task<Message?> NextAfter(long? id = null, string? topicPrefix = null);
}
