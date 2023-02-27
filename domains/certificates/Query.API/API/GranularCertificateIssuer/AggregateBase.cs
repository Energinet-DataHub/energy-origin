using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace API.GranularCertificateIssuer;

// Infrastructure to capture modifications to state in events
public abstract class AggregateBase
{
    // For indexing our event streams
    public Guid Id { get; protected set; }

    // For protecting the state, i.e. conflict prevention
    // The setter is only public for setting up test conditions
    public long Version { get; set; }

    // JsonIgnore - for making sure that it won't be stored in inline projection
    [JsonIgnore]
    private readonly List<object> uncommittedEvents = new();

    // Get the deltas, i.e. events that make up the state, not yet persisted
    public IEnumerable<object> GetUncommittedEvents() => uncommittedEvents;

    // Mark the deltas as persisted.
    public void ClearUncommittedEvents() => uncommittedEvents.Clear();

    // add the event to the uncommitted list
    protected void AddUncommittedEvent(object @event) => uncommittedEvents.Add(@event);
}
