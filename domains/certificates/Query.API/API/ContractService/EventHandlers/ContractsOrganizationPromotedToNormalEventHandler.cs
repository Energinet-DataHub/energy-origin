using System;
using System.Threading.Tasks;
using API.ContractService.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.ContractService.EventHandlers;

public class ContractsOrganizationPromotedToNormalEventHandler : IConsumer<OrganizationPromotedToNormal>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContractsOrganizationPromotedToNormalEventHandler> _logger;

    public ContractsOrganizationPromotedToNormalEventHandler(IMediator mediator, ILogger<ContractsOrganizationPromotedToNormalEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrganizationPromotedToNormal> context)
    {
        var orgId = context.Message.OrganizationId;

        _logger.LogInformation("Organization promoted to normal from trial, removing all contracts and slidingWindows");
        var removeContractsAndSlidingWindowsCommand = new RemoveOrganizationContractsAndSlidingWindowsCommand(orgId);
        await _mediator.Send(removeContractsAndSlidingWindowsCommand, context.CancellationToken);
    }
}

public class ContractsOrganizationPromotedToProductionEventHandlerDefinition : ConsumerDefinition<ContractsOrganizationPromotedToNormalEventHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContractsOrganizationPromotedToNormalEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
