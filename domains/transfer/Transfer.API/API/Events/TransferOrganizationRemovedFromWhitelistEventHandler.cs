using System;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MediatR;

namespace API.Events;

public class TransferOrganizationRemovedFromWhitelistEventHandler : IConsumer<OrganizationRemovedFromWhitelist>
{
    private readonly IMediator _mediator;

    public TransferOrganizationRemovedFromWhitelistEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OrganizationRemovedFromWhitelist> context)
    {
        var e = context.Message;

        var cmd = new DeleteTransferAgreementsCommand(OrganizationId.Create(e.OrganizationId));
        await _mediator.Send(cmd);
    }
}

public class TransferOrganizationRemovedFromWhitelistEventHandlerDefinition : ConsumerDefinition<TransferOrganizationRemovedFromWhitelistEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TransferOrganizationRemovedFromWhitelistEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
