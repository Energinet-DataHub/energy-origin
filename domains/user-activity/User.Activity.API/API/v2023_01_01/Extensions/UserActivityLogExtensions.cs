using API.Models;
using API.v2023_01_01.Dto.Responses;

namespace API.v2023_01_01.Extensions;

public static class UserActivityLogExtensions
{
    public static UserActivityLogDto ToDto(this UserActivityLog log)
    {
        return new UserActivityLogDto(
            Id: log.Id,
            ActorId: log.ActorId,
            EntityType: log.EntityType,
            ActivityDate: log.ActivityDate.ToUnixTimeSeconds(),
            OrganizationId: log.OrganizationId,
            Tin: log.Tin,
            OrganizationName: log.OrganizationName
        );
    }
}
