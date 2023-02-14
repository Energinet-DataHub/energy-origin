using System;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.DemoWorkflow;

public class DemoStateMachine : MassTransitStateMachine<DemoStateMachineInstance>
{
    public Event<DemoRequested> DemoRequested { get; private set; }

    public DemoStateMachine(ILogger<DemoStateMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => DemoRequested, c => c.CorrelateById(x => x.Message.CorrelationId));

        Initially(
            When(DemoRequested)
                .Then(x => logger.LogInformation("Received {message}", x.CorrelationId)));    
    }
}

public class DemoStateMachineInstance : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
}
