using System;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Events;

public class ReportRequestCreatedEventHandler : IConsumer<ReportRequestCreated>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportRequestCreatedEventHandler> _logger;

    public ReportRequestCreatedEventHandler(
        IMediator mediator,
        ILogger<ReportRequestCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ReportRequestCreated> context)
    {
        var e = context.Message;

        _logger.LogInformation(
            "Report request received for ReportId={ReportId}, Start={Start}, End={End}",
            e.ReportId,
            UnixTimestamp.Create(e.StartDate),
            UnixTimestamp.Create(e.EndDate));

        // TODO: Implement real handling in next user story
        return Task.CompletedTask;
    }
}

public class ReportRequestCreatedEventHandlerDefinition :
    ConsumerDefinition<ReportRequestCreatedEventHandler>
{
    public ReportRequestCreatedEventHandlerDefinition()
    {
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ReportRequestCreatedEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
