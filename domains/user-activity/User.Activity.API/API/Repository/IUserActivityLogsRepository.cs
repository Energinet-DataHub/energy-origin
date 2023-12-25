﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;
using API.Repository.Dto;

namespace API.Repository;

public interface IUserActivityLogsRepository
{
    Task<UserActivityLogResult> GetUserActivityLogsAsync(
        Guid actorId,
        List<EntityType> entityTypes,
        DateTime? startDate,
        DateTime? endDate,
        Pagination pagination
        );
}
