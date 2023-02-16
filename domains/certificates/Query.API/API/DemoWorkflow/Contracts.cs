using System;

namespace API.DemoWorkflow;

#region Events

public record DemoRequested
{
    public Guid CorrelationId { get; init; }
    public string Foo { get; init; } = "";
}

public record DemoInRegistrySaved
{
    public Guid CorrelationId { get; init; }
    public string Foo { get; init; } = "";
    public int R { get; set; }
}

#endregion

#region Commands

public record SaveDemoInRegistry
{
    public Guid CorrelationId { get; init; }
    public string Foo { get; init; } = "";
}

#endregion

#region Requests and repsonses

public record DemoStatusRequest
{
    public Guid CorrelationId { get; init; }
}

public record DemoStatusResponse
{
    public Guid CorrelationId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Status { get; init; } = "";
}

public record NotFoundResponse
{
    public Guid CorrelationId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

#endregion
