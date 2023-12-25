using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using API.Repository.Dto;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public class UserActivityLogsRepository(DbContext context) : IUserActivityLogsRepository
{
    public async Task<UserActivityLogResult> GetUserActivityLogsAsync(Guid actorId, List<EntityType> entityTypes, DateTime? startDate, DateTime? endDate, Pagination pagination)
    {
        var dbQuery = context.Set<UserActivityLog>().AsQueryable();

        dbQuery = FilterByActorId(dbQuery, actorId);
        dbQuery = FilterByEntityTypes(dbQuery, entityTypes);
        dbQuery = FilterByStartDate(dbQuery, startDate);
        dbQuery = FilterByEndDate(dbQuery, endDate);

        var totalCount = await dbQuery.CountAsync();
        var paginatedLogs = await dbQuery.Skip(pagination.offset).Take(pagination.limit).ToListAsync();
        return new UserActivityLogResult(totalCount, paginatedLogs);
    }

    private IQueryable<UserActivityLog> FilterByActorId(IQueryable<UserActivityLog> query, Guid actorId)
    {
        return query.Where(log => log.ActorId == actorId);
    }

    private IQueryable<UserActivityLog> FilterByEntityTypes(IQueryable<UserActivityLog> query, List<EntityType> entityTypes)
    {
        return entityTypes.Count != 0 ? query.Where(log => entityTypes.Contains(log.EntityType)) : query;
    }

    private IQueryable<UserActivityLog> FilterByStartDate(IQueryable<UserActivityLog> query, DateTime? startDate)
    {
        return startDate.HasValue ? query.Where(log => log.ActivityDate >= startDate.Value) : query;
    }

    private IQueryable<UserActivityLog> FilterByEndDate(IQueryable<UserActivityLog> query, DateTime? endDate)
    {
        return endDate.HasValue ? query.Where(log => log.ActivityDate <= endDate.Value) : query;
    }
}
