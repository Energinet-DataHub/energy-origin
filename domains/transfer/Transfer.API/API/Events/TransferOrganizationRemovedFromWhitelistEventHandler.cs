using System;
using System.Threading.Tasks;
using API.Options;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;

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
    private readonly RetryOptions _retryOptions;

    public TransferOrganizationRemovedFromWhitelistEventHandlerDefinition(IOptions<RetryOptions> retryOptions)
    {
        _retryOptions = retryOptions.Value;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TransferOrganizationRemovedFromWhitelistEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(_retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
