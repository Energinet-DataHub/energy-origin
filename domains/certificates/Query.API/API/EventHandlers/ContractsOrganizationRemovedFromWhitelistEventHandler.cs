using System;
using System.Threading.Tasks;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.EventHandlers;
public record EnqueueContractAndSlidingWindowDeletionTaskMessage(Guid OrganizationId);

public class ContractsOrganizationRemovedFromWhitelistEventHandler(
    ILogger<ContractsOrganizationRemovedFromWhitelistEventHandler> logger,
    IPublishEndpoint publishEndpoint)
    : IConsumer<OrganizationRemovedFromWhitelist>
{
    public async Task Consume(ConsumeContext<OrganizationRemovedFromWhitelist> context)
    {
        var orgId = context.Message.OrganizationId;

        logger.LogInformation("Publishing EnqueueContractAndSlidingWindowDeletionTaskMessage for OrganizationId {OrganizationId} via transactional outbox", orgId);
        await publishEndpoint.Publish(new EnqueueContractAndSlidingWindowDeletionTaskMessage(orgId), context.CancellationToken);
    }
}

public class OrganizationRemovedFromWhitelistEventHandlerDefinition : ConsumerDefinition<ContractsOrganizationRemovedFromWhitelistEventHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ContractsOrganizationRemovedFromWhitelistEventHandler> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
