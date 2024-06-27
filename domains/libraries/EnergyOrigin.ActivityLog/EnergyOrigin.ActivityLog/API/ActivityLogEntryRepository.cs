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
    }

    public async Task<IList<ActivityLogEntry>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsNoTracking().AsQueryable();

        if (request.Start is not null)
        {
            var mappedStart = DateTimeOffset.FromUnixTimeSeconds(request.Start.Value);
            activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= mappedStart);
        }

        if (request.End is not null)
        {
            var mappedEnd = DateTimeOffset.FromUnixTimeSeconds(request.End.Value);
            activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= mappedEnd);
        }

        if (request.EntityType is not null)
        {
            var mappedEntityType = ActivityLogExtensions.EntityTypeMapper(request.EntityType.Value);
            activityLogQuery = activityLogQuery.Where(x => x.EntityType == mappedEntityType);
        }

        return await activityLogQuery.Where(l => l.OrganizationTin == tin)
            .OrderBy(x => x.Timestamp)
            .Take(101)
            .ToListAsync();
    }
}
