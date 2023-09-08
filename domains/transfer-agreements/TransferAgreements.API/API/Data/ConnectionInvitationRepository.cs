using System;
using System.Linq;
using System.Threading.Tasks;
using API.Models;

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

    public async Task<int> DeleteOldConnectionInvitations(TimeSpan olderThan)
    {
        var cutoffDate = DateTimeOffset.Now - olderThan;
        var oldConnectionInvitations = context.ConnectionInvitations
            .Where(i => i.CreatedAt < cutoffDate);

        context.ConnectionInvitations.RemoveRange(oldConnectionInvitations);

        return await context.SaveChangesAsync();
    }
}
