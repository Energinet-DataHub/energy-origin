using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using API.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class UserActivityLogsRepository(ApplicationDbContext context) : IUserActivityLogsRepository
{
    public async Task<List<UserActivityLog>> GetUserActivityLogsAsync(Guid actorId)
    {
        return await context.Set<UserActivityLog>()
            .Where(log => log.ActorId == actorId)
            .ToListAsync();
    }
}
