using API.Connection.Api.Repository;
using API.Connection.Api.Services.ConnectionInvitationCleanup;
using Microsoft.Extensions.DependencyInjection;

namespace API.Connection;

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
