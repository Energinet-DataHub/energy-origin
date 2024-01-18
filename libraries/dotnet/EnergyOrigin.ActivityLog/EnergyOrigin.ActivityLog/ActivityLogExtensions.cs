using System.Text.Json;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
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
        services.AddScoped<ActivityLogEntryRepository>();

        return services;
    }

    public static RouteHandlerBuilder UseActivityLog(this IEndpointRouteBuilder builder)
    {
        return builder.MapPost(
            "/activity-log",
            async (ActivityLogEntryFilterRequest request, ActivityLogEntryRepository activityLogEntryRepository)
                => await activityLogEntryRepository.GetActivityLogAsync(request))
            .ExcludeFromDescription()
            .RequireAuthorization();
    }

    public static void AddActivityLogEntry(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ActivityLogEntry>()
            .HasKey(x => x.OrganizationTin).IsClustered(clustered: false);
    }
}
