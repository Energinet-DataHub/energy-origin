using System.Threading.Tasks;
using MassTransit;
using MassTransitContracts.Contracts;

namespace API.Transfer.Api.Services;

public interface IUserActivityEventPublisher
{
    Task PublishUserActivityEvent(UserActivityEvent userActivityEvent);
}

public class UserActivityEventPublisher : IUserActivityEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public UserActivityEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishUserActivityEvent(UserActivityEvent userActivityEvent)
    {
        await _publishEndpoint.Publish(userActivityEvent);
    }
}

