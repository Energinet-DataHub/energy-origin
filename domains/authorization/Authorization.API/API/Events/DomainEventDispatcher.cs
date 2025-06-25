using System.Collections.Generic;
using System.Threading.Tasks;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToNormal.V1;
using MassTransit;

namespace API.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public DomainEventDispatcher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            if (domainEvent is OrganizationPromotedToNormalDomainEvent promotedToNormal)
            {
                await _publishEndpoint.Publish<OrganizationPromotedToNormal>(
                    OrganizationPromotedToNormal.Create(promotedToNormal.OrganizationId.Value));
            }
        }
    }
}
