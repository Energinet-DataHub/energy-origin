namespace EnergyOrigin.IntegrationEvents.Events;

public abstract record IntegrationEvent
{
    protected IntegrationEvent(Guid id, Guid traceId, DateTimeOffset created)
    {
        Id = id;
        TraceId = traceId;
        Created = created;
    }

    public Guid Id { get; }
    public Guid TraceId { get; }
    public DateTimeOffset Created { get; }
}
