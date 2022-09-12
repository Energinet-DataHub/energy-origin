namespace EnergyOriginEventStore.EventStore.Database;

public record Message(long? Id, string Topic, string Payload);
