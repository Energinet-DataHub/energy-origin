using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext context;

    public ConnectionRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public Task<List<Connection>> GetCompanyConnections(Guid companyId) =>
        context.Connections
            .Where(x => x.CompanyAId == companyId || x.CompanyBId == companyId)
            .ToListAsync();

    public async Task<Connection?> GetConnection(Guid id)
    {
        return await context.Connections.FindAsync(id);
    }

    public async Task DeleteConnection(Guid id)
    {
        var connection = await GetConnection(id);
        if (connection != null)
        {
            context.Connections.Remove(connection);
            await context.SaveChangesAsync();
        }
    }
}
