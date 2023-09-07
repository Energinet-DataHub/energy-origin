using System;
using System.Linq;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public class InvitationRepository : IInvitationRepository
{
    private readonly ApplicationDbContext context;

    public InvitationRepository(ApplicationDbContext context) => this.context = context;

    public async Task<Invitation> AddInvitation(Invitation invitation)
    {
        context.Invitations.Add(invitation);
        await context.SaveChangesAsync();
        return invitation;
    }

    public async Task<int> DeleteOldInvitations(TimeSpan olderThan)
    {
        var cutoffDate = DateTimeOffset.Now - olderThan;
        var oldInvitations = context.Invitations
            .Where(i => i.CreatedAt < cutoffDate);

        context.Invitations.RemoveRange(oldInvitations);

        return await context.SaveChangesAsync();
    }
}
