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

    public async Task<bool> HasConflict(Guid currentCompanyId, Guid senderCompanyId)
    {
        var existingConnections = await GetCompanyConnections(currentCompanyId);
        return IsConflict(existingConnections, senderCompanyId);
    }

    private static bool IsConflict(List<Connection> existingConnections, Guid senderCompanyId) =>
        existingConnections.Any(c => c.CompanyAId == senderCompanyId || c.CompanyBId == senderCompanyId);
}
