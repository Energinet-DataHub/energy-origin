using System;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.DemoWorkflow;

public class DemoStateMachine : MassTransitStateMachine<DemoStateMachineInstance>
{
    public State? Processing { get; set; }

    public Event<DemoRequested>? DemoRequested { get; set; }
    public Event<DemoStatusRequest>? StatusRequested { get; set; }

    public DemoStateMachine(ILogger<DemoStateMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => DemoRequested);

        Event(() => StatusRequested, c =>
        {
            c.CorrelateById(x => x.Message.CorrelationId);
            c.OnMissingInstance(context => context.ExecuteAsync(x => x.RespondAsync(
                new NotFoundResponse
                {
                    CorrelationId = x.Message.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow
                })));
        });

        Initially(
            When(DemoRequested)
                .Then(x => logger.LogInformation("Received {correlationId}", x.CorrelationId))
                .TransitionTo(Processing));

        During(Processing,
            When(StatusRequested)
                .Then(x => logger.LogInformation("Status {correlationId}", x.CorrelationId))
                .Respond(c => new DemoStatusResponse
                {
                    CorrelationId = c.Message.CorrelationId,
                    Timestamp = DateTimeOffset.Now,
                    Status = "Running"
                }));
    }
}

public class DemoStateMachineInstance : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "Initial";
}
