using System;
using System.Threading.Tasks;
using API.ContractService.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.ContractService.EventHandlers;

public class ContractsOrganizationRemovedFromWhitelistEventHandler(
    ILogger<ContractsOrganizationRemovedFromWhitelistEventHandler> logger,
    IMediator mediator)
    : IConsumer<OrganizationRemovedFromWhitelist>
{
    public async Task Consume(ConsumeContext<OrganizationRemovedFromWhitelist> context)
    {
        var orgId = context.Message.OrganizationId;

        logger.LogInformation("Organization {orgId} removed from whitelist, removing all contracts and slidingWindows", orgId);
        var removeContractsAndSlidingWindowsCommand = new RemoveOrganizationContractsAndSlidingWindowsCommand(orgId);
        await mediator.Send(removeContractsAndSlidingWindowsCommand, context.CancellationToken);
    }
}

public class ContractsOrganizationRemovedFromWhitelistEventHandlerDefinition : ConsumerDefinition<ContractsOrganizationRemovedFromWhitelistEventHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ContractsOrganizationRemovedFromWhitelistEventHandler> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
