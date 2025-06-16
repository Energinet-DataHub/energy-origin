using System;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Events;

public class TransferOrganizationPromotedToNormalEventHandler : IConsumer<OrganizationPromotedToNormal>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransferOrganizationPromotedToNormalEventHandler> _logger;

    public TransferOrganizationPromotedToNormalEventHandler(IMediator mediator, ILogger<TransferOrganizationPromotedToNormalEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrganizationPromotedToNormal> context)
    {
        var e = context.Message;

        var orgId = OrganizationId.Create(e.OrganizationId);

        _logger.LogInformation("Organization promoted to normal from trial, removing all transfer agreements for organization");
        var deleteTasCmd = new DeleteTransferAgreementsCommand(orgId);
        await _mediator.Send(deleteTasCmd);

        _logger.LogInformation("Organization promoted to normal from trial, deleting claim automation arguments for organization");
        var deleteArgsCmd = new DeleteClaimAutomationArgsCommand(orgId);
        await _mediator.Send(deleteArgsCmd);
    }
}

public class TransferOrganizationPromotedToProductionEventHandlerDefinition : ConsumerDefinition<TransferOrganizationPromotedToNormalEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TransferOrganizationPromotedToNormalEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
