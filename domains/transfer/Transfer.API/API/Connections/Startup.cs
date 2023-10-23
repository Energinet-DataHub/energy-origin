using API.Connections.Api.Repository;
using API.Connections.Automation.ConnectionInvitationCleanup;
using Microsoft.Extensions.DependencyInjection;

namespace API.Connections;

public static class Startup
{
    public static void AddConnection(this IServiceCollection services)
    {
        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddHostedService<ConnectionInvitationCleanupWorker>();
        services.AddScoped<IConnectionInvitationRepository, ConnectionInvitationRepository>();
        services.AddScoped<IConnectionInvitationCleanupService, ConnectionInvitationCleanupService>();
    }

}
