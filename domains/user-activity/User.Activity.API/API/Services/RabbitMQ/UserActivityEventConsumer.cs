using System.Threading.Tasks;
using API.Models;
using API.Models.RabbitMQ;
using API.Shared.Data;
using MassTransit;

namespace API.Services.RabbitMQ;

public class UserActivityEventConsumer(ApplicationDbContext context) : IConsumer<UserActivityEvent>
{
    public async Task Consume(ConsumeContext<UserActivityEvent> context1)
    {
        var message = context1.Message;

        var userActivityLog = new UserActivityLog(
            Id: message.Id,
            ActorId: message.ActorId,
            EntityType: message.EntityType,
            ActivityDate: message.ActivityDate,
            OrganizationId: message.OrganizationId,
            Tin: message.Tin,
            OrganizationName: message.OrganizationName ?? ""
        );

        await context.UserActivityLogs.AddAsync(userActivityLog);
        await context.SaveChangesAsync();
    }
}


