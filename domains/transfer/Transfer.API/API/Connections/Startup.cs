using API.Connections.Api.Repository;
using API.Connections.Api.Services.ConnectionInvitationCleanup;
using Microsoft.Extensions.DependencyInjection;

namespace API.Connections;

public static class Startup
{
    public static void AddConnections(this IServiceCollection services)
    {
        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IConnectionInvitationRepository, ConnectionInvitationRepository>();
        services.AddScoped<IConnectionInvitationCleanupService, ConnectionInvitationCleanupService>();
        services.AddHostedService<ConnectionInvitationCleanupWorker>();
    }

}
