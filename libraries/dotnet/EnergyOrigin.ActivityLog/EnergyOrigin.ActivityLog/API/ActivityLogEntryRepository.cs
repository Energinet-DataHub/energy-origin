using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EnergyOrigin.ActivityLog.API;

public interface IActivityLogEntryRepository
{
    Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry);
    Task<List<ActivityLogEntryResponse>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request);
}

public class ActivityLogEntryRepository(DbContext dbContext) : IActivityLogEntryRepository
{
    public async Task AddActivityLogEntryAsync(ActivityLogEntry activityLogEntry)
    {
        await dbContext.Set<ActivityLogEntry>().AddAsync(activityLogEntry);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<ActivityLogEntryResponse>> GetActivityLogAsync(string tin, ActivityLogEntryFilterRequest request)
    {
        var activityLogQuery = dbContext.Set<ActivityLogEntry>().AsQueryable();

        if(request.Start != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp >= request.Start);
        if(request.End != null) activityLogQuery = activityLogQuery.Where(x => x.Timestamp <= request.End);
        if (request.EntityType != null) activityLogQuery = activityLogQuery.Where(x => x.EntityType == default); // TODO MAP

        var response = await activityLogQuery.Where(l => l.OrganizationTin == tin).ToListAsync();

        return response.Select(x =>
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
            }
        ).ToList();
    }

    private ActivityLogEntryResponse.ActionTypeEnum ActionTypeMapper(ActivityLogEntry.ActionTypeEnum actionType) =>
        actionType switch
        {
            ActivityLogEntry.ActionTypeEnum.Created => ActivityLogEntryResponse.ActionTypeEnum.Created,
            ActivityLogEntry.ActionTypeEnum.Accepted => ActivityLogEntryResponse.ActionTypeEnum.Accepted,
            ActivityLogEntry.ActionTypeEnum.Declined => ActivityLogEntryResponse.ActionTypeEnum.Declined,
            ActivityLogEntry.ActionTypeEnum.Activated => ActivityLogEntryResponse.ActionTypeEnum.Activated,
            ActivityLogEntry.ActionTypeEnum.Deactivated => ActivityLogEntryResponse.ActionTypeEnum.Deactivated,
            ActivityLogEntry.ActionTypeEnum.ChangeEndDate => ActivityLogEntryResponse.ActionTypeEnum.ChangeEndDate,
            _ => throw new NotImplementedException()
        };

    private ActivityLogEntryResponse.ActorTypeEnum ActorTypeMapper(ActivityLogEntry.ActorTypeEnum actorType) =>
        actorType switch
        {
            ActivityLogEntry.ActorTypeEnum.User => ActivityLogEntryResponse.ActorTypeEnum.User,
            ActivityLogEntry.ActorTypeEnum.System => ActivityLogEntryResponse.ActorTypeEnum.System,
            _ => throw new NotImplementedException()
        };

    private ActivityLogEntryResponse.EntityTypeEnum EntityTypeMapper(ActivityLogEntry.EntityTypeEnum entityType) =>
        entityType switch
        {
            ActivityLogEntry.EntityTypeEnum.TransferAgreement => ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.EntityTypeEnum.MeteringPoint => ActivityLogEntryResponse.EntityTypeEnum.MeteringPoint,
            _ => throw new NotImplementedException()
        };
}

