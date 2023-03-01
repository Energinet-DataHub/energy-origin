using System;
using System.Collections.Generic;

namespace CertificateEvents.Aggregates;

// Infrastructure to capture modifications to state in events
public abstract class AggregateBase
{
    public Guid Id { get; protected set; }

    // For protecting the state, i.e. conflict prevention
    // The setter is only public for setting up test conditions
    public long Version { get; protected set; }

    private readonly List<object> uncommittedEvents = new();

    public IEnumerable<object> GetUncommittedEvents() => uncommittedEvents;

    public void ClearUncommittedEvents() => uncommittedEvents.Clear();

    protected void AddUncommittedEvent(object @event) => uncommittedEvents.Add(@event);
}
