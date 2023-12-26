using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;
using API.Repository.Dto;
using MassTransitContracts.Contracts;

namespace API.Repository;

public interface IUserActivityLogsRepository
{
    Task<UserActivityLogResult> GetUserActivityLogsAsync(Guid actorId,
        List<EntityType> entityTypes,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        Pagination pagination);
}
