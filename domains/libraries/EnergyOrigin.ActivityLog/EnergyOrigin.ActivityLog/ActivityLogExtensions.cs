using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.ActivityLog.HostedService;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    public static RouteHandlerBuilder UseActivityLogWithB2CSupport(this IEndpointRouteBuilder builder)
    {
        var options = builder.ServiceProvider.GetRequiredService<IOptions<ActivityLogOptions>>();
        var serviceName = options.Value.ServiceName;

        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        return builder.MapPost(
                $"api/{serviceName}/activity-log",
                async ([FromBody] ActivityLogEntryFilterRequest request, [FromServices] IHttpContextAccessor httpContextAccessor,
                    [FromServices] IActivityLogEntryRepository activityLogEntryRepository) =>
                {
                    var identityDescriptor = new IdentityDescriptor(httpContextAccessor);
                    return await GetActivityLogFromCvr(activityLogEntryRepository, identityDescriptor.OrganizationCvr!, request);
                })
            .WithTags("Activity log")
            .ExcludeFromDescription()
            .RequireAuthorization(Policy.Frontend);
    }

    private static async Task<ActivityLogListEntryResponse> GetActivityLogFromCvr(IActivityLogEntryRepository activityLogEntryRepository, string cvr,
        ActivityLogEntryFilterRequest request)
    {
        var activityLogEntries = await activityLogEntryRepository.GetActivityLogAsync(cvr, request);
        return new ActivityLogListEntryResponse
        {
            ActivityLogEntries = activityLogEntries.Select(x =>
                new ActivityLogEntryResponse
                {
                    Id = x.Id,
                    OrganizationTin = x.OrganizationTin,
                    EntityId = x.EntityId,
                    Timestamp = x.Timestamp.ToUnixTimeSeconds(),
                    ActorName = x.ActorName,
                    ActorId = x.ActorId,
                    OrganizationName = x.OrganizationName,
                    OtherOrganizationTin = x.OtherOrganizationTin,
                    OtherOrganizationName = x.OtherOrganizationName,
                    EntityType = EntityTypeMapper(x.EntityType),
                    ActorType = ActorTypeMapper(x.ActorType),
                    ActionType = ActionTypeMapper(x.ActionType)
                }).Take(100),
            HasMore = activityLogEntries.Count > 100
        };
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
            _ => throw new InvalidEnumArgumentException()
        };

    public static ActivityLogEntryResponse.ActorTypeEnum ActorTypeMapper(ActivityLogEntry.ActorTypeEnum actorType) =>
        actorType switch
        {
            ActivityLogEntry.ActorTypeEnum.User => ActivityLogEntryResponse.ActorTypeEnum.User,
            ActivityLogEntry.ActorTypeEnum.System => ActivityLogEntryResponse.ActorTypeEnum.System,
            _ => throw new InvalidEnumArgumentException()
        };

    public static ActivityLogEntryResponse.EntityTypeEnum EntityTypeMapper(ActivityLogEntry.EntityTypeEnum entityType) =>
        entityType switch
        {
            ActivityLogEntry.EntityTypeEnum.TransferAgreement => ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.EntityTypeEnum.MeteringPoint => ActivityLogEntryResponse.EntityTypeEnum.MeteringPoint,
            ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal => ActivityLogEntryResponse.EntityTypeEnum.TransferAgreementProposal,
            _ => throw new InvalidEnumArgumentException()
        };

    public static ActivityLogEntry.EntityTypeEnum EntityTypeMapper(ActivityLogEntryResponse.EntityTypeEnum requestEntityType) =>
        requestEntityType switch
        {
            ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement => ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntryResponse.EntityTypeEnum.MeteringPoint => ActivityLogEntry.EntityTypeEnum.MeteringPoint,
            ActivityLogEntryResponse.EntityTypeEnum.TransferAgreementProposal => ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
            _ => throw new InvalidEnumArgumentException()
        };
}
