using System;
using MassTransit;

namespace API.DemoWorkflow;

#region Events

public record DemoRequested
{
    public Guid CorrelationId { get; init; }
    public string Foo { get; init; } = "";
}

#endregion
