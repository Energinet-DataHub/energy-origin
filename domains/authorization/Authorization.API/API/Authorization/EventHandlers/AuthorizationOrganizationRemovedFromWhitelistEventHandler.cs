using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist;
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
    }
}
