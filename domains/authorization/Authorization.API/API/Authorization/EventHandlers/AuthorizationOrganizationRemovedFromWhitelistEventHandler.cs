using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.Authorization._Features_.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Authorization.EventHandlers;

public class AuthorizationOrganizationRemovedFromWhitelistEventHandler(ILogger<AuthorizationOrganizationRemovedFromWhitelistEventHandler> logger, IMediator mediator)
    : IConsumer<OrganizationRemovedFromWhitelist>
{
    public async Task Consume(ConsumeContext<OrganizationRemovedFromWhitelist> context)
    {
        var orgId = context.Message.OrganizationId;

        logger.LogInformation("Organization {orgId} removed from whitelist, removing all consents", orgId);
        var removeOrganizationConsentsCommand = new RemoveOrganizationConsentsCommand(orgId);
        await mediator.Send(removeOrganizationConsentsCommand, context.CancellationToken);

        logger.LogInformation("Organization {orgId} removed from whitelist, removing all clients", orgId);
        var removeOrganizationClientsCommand = new RemoveOrganizationClientsCommand(orgId);
        await mediator.Send(removeOrganizationClientsCommand, context.CancellationToken);

        logger.LogInformation("Organization {orgId} removed from whitelist, revoking acceptance of terms", orgId);
        var revokeTermsCommand = new RevokeTermsCommand(orgId);
        await mediator.Send(revokeTermsCommand, context.CancellationToken);
    }
}

public class AuthorizationOrganizationRemovedFromWhitelistEventHandlerDefinition : ConsumerDefinition<
    AuthorizationOrganizationRemovedFromWhitelistEventHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<AuthorizationOrganizationRemovedFromWhitelistEventHandler> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
