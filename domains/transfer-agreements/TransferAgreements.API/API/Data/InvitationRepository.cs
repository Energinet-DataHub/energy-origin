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
}
