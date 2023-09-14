using System;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ConnectionInvitationRepository : IConnectionInvitationRepository
{
    private readonly ApplicationDbContext context;

    public ConnectionInvitationRepository(ApplicationDbContext context) => this.context = context;

    public async Task AddConnectionInvitation(ConnectionInvitation connectionInvitation)
    {
        context.ConnectionInvitations.Add(connectionInvitation);
        await context.SaveChangesAsync();
    }

    public async Task DeleteOldConnectionInvitations(DateTimeOffset olderThan)
    {
        var oldConnectionInvitations = context.ConnectionInvitations
            .Where(i => i.CreatedAt < olderThan);

        context.ConnectionInvitations.RemoveRange(oldConnectionInvitations);

        await context.SaveChangesAsync();
    }

    public async Task<ConnectionInvitation?> GetNonExpiredConnectionInvitation(Guid id)
    {
        var connectionInvitation = await context.ConnectionInvitations
            .FirstOrDefaultAsync(i => i.CreatedAt < DateTimeOffset.UtcNow.AddDays(-14) && i.Id == id);

        return connectionInvitation;
    }
}
