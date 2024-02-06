using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.ActivityLog.HostedService;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.ActivityLog;

public static class ActivityLogExtensions
{
    public static IServiceCollection AddActivityLog(this IServiceCollection services, Action<ActivityLogOptions> options)
    {
        services.AddScoped<IActivityLogEntryRepository, ActivityLogEntryRepository>();
        services.AddHostedService<CleanupActivityLogsHostedService>();

        services.Configure(options);

        return services;
    }

    public static RouteHandlerBuilder UseActivityLog(this IEndpointRouteBuilder builder)
    {
        var options = builder.ServiceProvider.GetRequiredService<IOptions<ActivityLogOptions>>();
        var serviceName = options.Value.ServiceName;

        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        return builder.MapPost(
            $"api/{serviceName}/activity-log",
            async (HttpContext HttpContext, ActivityLogEntryFilterRequest request, IActivityLogEntryRepository activityLogEntryRepository)
                =>
            {
                var user = new UserDescriptor(HttpContext.User);
                var activityLogEntries =
                    await activityLogEntryRepository.GetActivityLogAsync(user.Organization!.Tin, request);
                return new ActivityLogListEntryResponse
                {
                    ActivityLogEntries = activityLogEntries.Select(x =>
                        new ActivityLogEntryResponse
                        {
                            Id = x.Id,
                            OrganizationTin = x.OrganizationTin,
                            EntityId = x.EntityId,
                            Timestamp = x.Timestamp,
                            ActorName = x.ActorName,
                            ActorId = x.ActorId,
                            OrganizationName = x.OrganizationName,
                            EntityType = EntityTypeMapper(x.EntityType),
                            ActorType = ActorTypeMapper(x.ActorType),
                            ActionType = ActionTypeMapper(x.ActionType)
                        }).Take(100),
                    HasMore = activityLogEntries.Count > 100
                };
            }).WithTags("Activity log").RequireAuthorization();
    }

    public static void AddActivityLogEntry(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLogEntry>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ActivityLogEntry>()
            .HasIndex(x => x.OrganizationTin).IsClustered(clustered: false);
    }

    public static ActivityLogEntryResponse.ActionTypeEnum ActionTypeMapper(ActivityLogEntry.ActionTypeEnum actionType) =>
        actionType switch
        {
            ActivityLogEntry.ActionTypeEnum.Created => ActivityLogEntryResponse.ActionTypeEnum.Created,
            ActivityLogEntry.ActionTypeEnum.Accepted => ActivityLogEntryResponse.ActionTypeEnum.Accepted,
            ActivityLogEntry.ActionTypeEnum.Declined => ActivityLogEntryResponse.ActionTypeEnum.Declined,
            ActivityLogEntry.ActionTypeEnum.Activated => ActivityLogEntryResponse.ActionTypeEnum.Activated,
            ActivityLogEntry.ActionTypeEnum.Deactivated => ActivityLogEntryResponse.ActionTypeEnum.Deactivated,
            ActivityLogEntry.ActionTypeEnum.EndDateChanged => ActivityLogEntryResponse.ActionTypeEnum.EndDateChanged,
            ActivityLogEntry.ActionTypeEnum.Expired => ActivityLogEntryResponse.ActionTypeEnum.Expired,
            _ => throw new NotImplementedException()
        };

    public static ActivityLogEntryResponse.ActorTypeEnum ActorTypeMapper(ActivityLogEntry.ActorTypeEnum actorType) =>
        actorType switch
        {
            ActivityLogEntry.ActorTypeEnum.User => ActivityLogEntryResponse.ActorTypeEnum.User,
            ActivityLogEntry.ActorTypeEnum.System => ActivityLogEntryResponse.ActorTypeEnum.System,
            _ => throw new NotImplementedException()
        };

    public static ActivityLogEntryResponse.EntityTypeEnum EntityTypeMapper(ActivityLogEntry.EntityTypeEnum entityType) =>
        entityType switch
        {
            ActivityLogEntry.EntityTypeEnum.TransferAgreement => ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.EntityTypeEnum.MeteringPoint => ActivityLogEntryResponse.EntityTypeEnum.MeteringPoint,
            ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal => ActivityLogEntryResponse.EntityTypeEnum.TransferAgreementProposal,
            _ => throw new NotImplementedException()
        };
}
