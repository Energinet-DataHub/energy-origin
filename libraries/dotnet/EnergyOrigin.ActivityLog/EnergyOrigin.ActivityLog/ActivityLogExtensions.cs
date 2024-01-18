using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog;

public static class ActivityLogExtensions
{


    public static RouteHandlerBuilder UseActivityLogEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapPost("/activity-log", async (ActivityLogFilterRequest request, ActivityLogLogic activityLogLogic) =>
        {

            return await activityLogLogic.GetActivityLogAsync(request);
        }).ExcludeFromDescription().RequireAuthorization();
    }
}

// TODO: Better name and have interface :)
public class ActivityLogLogic
{
    private readonly DbContext _dbContext;

    public ActivityLogLogic(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task GetActivityLogAsync(ActivityLogFilterRequest request)
    {
        var queryable = _dbContext.Set<ActivityLogEntity>().AsQueryable();

        if(request.Start != null) queryable = queryable.Where(x => x.CreatedOnUtc >= request.Start);
        if(request.End != null) queryable = queryable.Where(x => x.CreatedOnUtc >= request.Start);



        throw new NotImplementedException();
    }
}

public record ActivityLogResponse(Guid Uid, DateTimeOffset CreatedOnUtc, string Type, string Description);

[Table("StudentMaster")]
public record ActivityLogEntity(Guid Uid, DateTimeOffset CreatedOnUtc, string Type, string Description);

public record ActivityLogFilterRequest(DateTimeOffset Start, DateTimeOffset End, string Type);
