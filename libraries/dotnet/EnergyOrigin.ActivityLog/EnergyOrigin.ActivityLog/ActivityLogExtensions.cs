using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.ActivityLog;

public static class ActivityLogExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        // add your services here, for example:


        return services;
    }

    public static RouteHandlerBuilder UseActivityLogEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapPost("/activity-log", async (ActivityLogFilterRequest request, ActivityLogRepository activityLogLogic) =>
        {

            return await activityLogLogic.GetActivityLogAsync(request);
        }).ExcludeFromDescription().RequireAuthorization();
    }
}

public class ActivityLogRepository(DbContext dbContext)
{
    public async Task<List<ActivityLogResponse>> GetActivityLogAsync(ActivityLogFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsQueryable();

        if(request.Start != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= request.Start);
        if(request.End != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= request.End);

        var response = await activityLogQuery.Select(x => new ActivityLogResponse()).ToListAsync();

        return response;
    }
}

public record ActivityLogFilterRequest(DateTimeOffset? Start, DateTimeOffset? End, string? Type);


[Table("ActivityLog")]
public class ActivityLogEntry
{
    public enum ActorTypeEnum { User, System }
    public enum EntityTypeEnum { TransferAgreement, MeteringPoint }
    public enum ActionTypeEnum { Created, Accepted, Declined, Activated, Deactivated, ChangeEndDate }

    // General
    public Guid Id { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    // User/Machine initiating the request
    public Guid ActorId { get; private set; } // Eks. Granular's id or Charlotte's id
    public ActorTypeEnum ActorType { get; private set; }
    public string ActorName { get; private set; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; private set; } = ""; // CVR
    public string OrganizationName { get; private set; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Action
    public EntityTypeEnum EntityType { get; private set; }
    public ActionTypeEnum ActionType { get; private set; }
    public Guid EntityId { get; private set; }
}

public class ActivityLogResponse
{
    public enum ActorTypeEnum { User, System }
    public enum EntityTypeEnum { TransferAgreement, MeteringPoint }
    public enum ActionTypeEnum { Created, Accepted, Declined, Activated, Deactivated, ChangeEndDate }

    // General
    public Guid Id { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    // User/Machine initiating the request
    public Guid ActorId { get; private set; } // Eks. Granular's id or Charlotte's id
    public ActorTypeEnum ActorType { get; private set; }
    public string ActorName { get; private set; } = ""; // Company name / person name

    // Owner
    public string OrganizationTin { get; private set; } = ""; // CVR
    public string OrganizationName { get; private set; } = ""; // Eks. "Mogens Mølleejer A/S"

    // Action
    public EntityTypeEnum EntityType { get; private set; }
    public ActionTypeEnum ActionType { get; private set; }
    public Guid EntityId { get; private set; }
}
