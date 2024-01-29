using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.API;

public interface IActivityLogEntryRepository
{
    Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry);
    Task<IList<ActivityLogEntry>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request);
}

public class ActivityLogEntryRepository(DbContext dbContext) : IActivityLogEntryRepository
{
    public async Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry)
    {
        await dbContext.Set<ActivityLogEntry>().AddAsync(activityLogEntry);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IList<ActivityLogEntry>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsNoTracking().AsQueryable();

        if (request.Start != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= request.Start);
        if (request.End != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= request.End);
        if (request.EntityType != null) activityLogQuery = activityLogQuery.Where(x => x.EntityType == default);

        return await activityLogQuery.Where(l => l.OrganizationTin == tin)
            .OrderBy(x => x.Timestamp)
            .Take(101)
            .ToListAsync();
    }
}
