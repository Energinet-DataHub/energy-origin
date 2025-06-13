using System;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Events;

public class TransferOrganizationPromotedToProductionEventHandler : IConsumer<OrganizationPromotedToNormal>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransferOrganizationPromotedToProductionEventHandler> _logger;

    public TransferOrganizationPromotedToProductionEventHandler(IMediator mediator, ILogger<TransferOrganizationPromotedToProductionEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrganizationPromotedToNormal> context)
    {
        var e = context.Message;

        var orgId = OrganizationId.Create(e.OrganizationId);

        _logger.LogInformation("Organization promoted to production from trial, removing all transfer agreements for organization");
        var deleteTasCmd = new DeleteTransferAgreementsCommand(orgId);
        await _mediator.Send(deleteTasCmd);

        _logger.LogInformation("Organization promoted to production from trial, deleting claim automation arguments for organization");
        var deleteArgsCmd = new DeleteClaimAutomationArgsCommand(orgId);
        await _mediator.Send(deleteArgsCmd);
    }
}

public class TransferOrganizationPromotedToProductionEventHandlerDefinition : ConsumerDefinition<TransferOrganizationPromotedToProductionEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TransferOrganizationPromotedToProductionEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
