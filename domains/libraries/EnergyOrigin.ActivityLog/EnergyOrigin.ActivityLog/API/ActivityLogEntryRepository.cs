using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.API;

public interface IActivityLogEntryRepository
{
    Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry);
    Task<IList<ActivityLogEntry>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request);
}
public class ActivityLogEntryRepository<TContext> : IActivityLogEntryRepository
    where TContext : DbContext
{
    private readonly TContext dbContext;

    public ActivityLogEntryRepository(TContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry)
    {
        await dbContext.Set<ActivityLogEntry>().AddAsync(activityLogEntry);
    }

    public async Task<IList<ActivityLogEntry>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsNoTracking().AsQueryable();

        if (request.Start is not null)
            activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= DateTimeOffset.FromUnixTimeSeconds(request.Start.Value));

        if (request.End is not null)
            activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= DateTimeOffset.FromUnixTimeSeconds(request.End.Value));

        if (request.EntityType is not null)
            activityLogQuery = activityLogQuery.Where(x => x.EntityType == ActivityLogExtensions.EntityTypeMapper(request.EntityType.Value));

        return await activityLogQuery
            .Where(l => l.OrganizationTin == tin)
            .OrderBy(x => x.Timestamp)
            .Take(101)
            .ToListAsync();
    }
}
