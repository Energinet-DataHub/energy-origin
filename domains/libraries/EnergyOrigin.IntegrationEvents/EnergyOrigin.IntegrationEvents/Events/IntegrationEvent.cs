using System.Diagnostics;

namespace EnergyOrigin.IntegrationEvents.Events;

public abstract record IntegrationEvent
{
    protected IntegrationEvent() : this(Guid.NewGuid(), Activity.Current?.Id ?? Guid.NewGuid().ToString(), DateTimeOffset.UtcNow)
    {

    }

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
