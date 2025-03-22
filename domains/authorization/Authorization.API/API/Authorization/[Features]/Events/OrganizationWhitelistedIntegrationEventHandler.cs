using System;
using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationWhitelisted;
using EnergyOrigin.Domain.ValueObjects;
using MassTransit;
using MediatR;

namespace API.Authorization._Features_.Events;

public class OrganizationWhitelistedIntegrationEventHandler : IConsumer<OrganizationWhitelistedIntegrationEvent>
{
    private readonly IMediator _mediator;

    public OrganizationWhitelistedIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OrganizationWhitelistedIntegrationEvent> context)
    {
        var tin = Tin.Create(context.Message.Tin);
        await _mediator.Send(new WhitelistOrganizationCommand(tin), context.CancellationToken);
    }
}


public class OrganizationWhitelistedIntegrationEventHandlerDefinition
    : ConsumerDefinition<OrganizationWhitelistedIntegrationEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrganizationWhitelistedIntegrationEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Incremental(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
    }
}

