using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.ActivityLog;

public static class ActivityLogExtensions
{
    public static IServiceCollection AddActivityLog(this IServiceCollection services)
    {
        services.AddScoped<IActivityLogEntryRepository, ActivityLogEntryRepository>();
        return services;
    }

    public static RouteHandlerBuilder UseActivityLog(this IEndpointRouteBuilder builder)
    {
        return builder.MapPost(
            "/activity-log",
            async (HttpContext HttpContext, ActivityLogEntryFilterRequest request, IActivityLogEntryRepository activityLogEntryRepository)
                =>
            {
                var user = new UserDescriptor(HttpContext.User);
                return await activityLogEntryRepository
                    .GetActivityLogAsync(user.Organization!.Tin, request);
            }).RequireAuthorization();
    }

    public static void AddActivityLogEntry(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ActivityLogEntry>()
            .HasIndex(x => x.OrganizationTin).IsClustered(clustered: false);
    }
}
