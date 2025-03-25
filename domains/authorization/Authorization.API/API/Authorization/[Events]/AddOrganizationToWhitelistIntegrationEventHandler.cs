using System;
using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.AddOrganizationToWhitelist;
using MassTransit;
using MediatR;

namespace API.Authorization._Events_;

public class AddOrganizationToWhitelistIntegrationEventHandler : IConsumer<AddOrganizationToWhitelistIntegrationEvent>
{
    private readonly IMediator _mediator;

    public AddOrganizationToWhitelistIntegrationEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<AddOrganizationToWhitelistIntegrationEvent> context)
    {
        var tin = Tin.Create(context.Message.Tin);
        await _mediator.Send(new AddOrganizationToWhitelistCommand(tin), context.CancellationToken);
    }
}


public class AddOrganizationToWhitelistIntegrationEventHandlerDefinition
    : ConsumerDefinition<AddOrganizationToWhitelistIntegrationEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AddOrganizationToWhitelistIntegrationEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Incremental(5, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
    }
}

