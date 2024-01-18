using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.API;

public class ActivityLogEntryRepository(DbContext dbContext)
{
    public async Task<List<ActivityLogResponse>> GetActivityLogAsync(ActivityLogEntryFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsQueryable();

        if(request.Start != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= request.Start);
        if(request.End != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= request.End);

        var response = await activityLogQuery.Select(x => new ActivityLogResponse()).ToListAsync();

        return response;
    }
}
