namespace EnergyOrigin.IntegrationEvents.Events;

public abstract record IntegrationEvent
{
    protected IntegrationEvent(Guid id, string traceId, DateTimeOffset created)
    {
        Id = id;
        TraceId = traceId;
        Created = created;
    }

    public Guid Id { get; }
    public string TraceId { get; }
    public DateTimeOffset Created { get; }
}
