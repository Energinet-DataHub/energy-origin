using System;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.DemoWorkflow;

public class DemoStateMachine : MassTransitStateMachine<DemoStateMachineInstance>
{
    public Event<DemoRequested>? DemoRequested { get; set; }

    public DemoStateMachine(ILogger<DemoStateMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => DemoRequested);

        Initially(
            When(DemoRequested)
                .Then(x => logger.LogInformation("Received {correlationId}", x.CorrelationId)));
    }
}

public class DemoStateMachineInstance : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "Initial";
}
