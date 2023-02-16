using System;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.DemoWorkflow;

public class DemoStateMachine : MassTransitStateMachine<DemoStateMachineInstance>
{
    public State? Processing { get; set; }

    public Event<DemoRequested>? DemoRequested { get; set; }
    public Event<DemoInRegistrySaved>? DemoInRegistrySaved { get; set; }
    public Event<DemoStatusRequest>? StatusRequested { get; set; }

    public DemoStateMachine(ILogger<DemoStateMachine> logger, DemoSendEndpointConfiguration endpoints)
    {
        InstanceState(x => x.CurrentState);

        Event(() => DemoRequested);
        Event(() => DemoInRegistrySaved);
        Event(() => StatusRequested, c =>
        {
            c.OnMissingInstance(context => context.ExecuteAsync(x => x.RespondAsync(
                new NotFoundResponse
                {
                    CorrelationId = x.Message.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow
                })));
        });

        During(Initial, Processing,
            When(DemoRequested)
                .Then(x => logger.LogInformation("Received {correlationId}", x.CorrelationId))
                .Send(endpoints.RegistryConnectorQueue, context => new SaveDemoInRegistry
                {
                    CorrelationId = context.Message.CorrelationId,
                    Foo = context.Message.Foo
                })
                .TransitionTo(Processing),
            When(DemoInRegistrySaved)
                .Then(x => logger.LogInformation("Received {correlationId}", x.CorrelationId))
                .Finalize());

        During(Processing,
            When(StatusRequested)
                .Then(x => logger.LogInformation("Status {correlationId}", x.CorrelationId))
                .Respond(c => new DemoStatusResponse
                {
                    CorrelationId = c.Message.CorrelationId,
                    Timestamp = DateTimeOffset.Now,
                    Status = "Processing"
                }));

        During(Final,
            When(StatusRequested)
                .Then(x => logger.LogInformation("Status {correlationId}", x.CorrelationId))
                .Respond(c => new DemoStatusResponse
                {
                    CorrelationId = c.Message.CorrelationId,
                    Timestamp = DateTimeOffset.Now,
                    Status = "Completed"
                }));
    }
}

public class DemoStateMachineInstance : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "Initial";
}
