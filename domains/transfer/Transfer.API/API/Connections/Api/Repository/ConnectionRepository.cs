using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Connections.Api.Models;
using API.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Connections.Api.Repository;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext context;

    public ConnectionRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task AddConnectionAndDeleteInvitation(Connection newConnection, Guid invitationId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.Connections.AddAsync(newConnection);

            var invitation = await context.ConnectionInvitations.FindAsync(invitationId);
            if (invitation != null)
            {
                context.ConnectionInvitations.Remove(invitation);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    public async Task<bool> HasConflict(Guid currentCompanyId, Guid senderCompanyId)
    {
        var existingConnections = await GetCompanyConnections(currentCompanyId);
        return IsConflict(existingConnections, senderCompanyId);
    }

    private static bool IsConflict(List<Connection> existingConnections, Guid senderCompanyId) =>
        existingConnections.Any(c => c.CompanyAId == senderCompanyId || c.CompanyBId == senderCompanyId);
}
