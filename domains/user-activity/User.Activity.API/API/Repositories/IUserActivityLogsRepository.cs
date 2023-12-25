using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repositories;

public interface IUserActivityLogsRepository
{
    Task<List<UserActivityLog>> GetUserActivityLogsAsync(Guid actorId);
}
