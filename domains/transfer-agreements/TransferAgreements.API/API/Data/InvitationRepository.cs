using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public class InvitationRepository : IInvitationRepository
{
    private readonly ApplicationDbContext context;

    public InvitationRepository(ApplicationDbContext context) => this.context = context;

    public async Task<Invitation> AddInvitationToDb(Invitation invitation)
    {
        context.Invitations.Add(invitation);
        await Save();
        return invitation;
    }
    private async Task Save() => await context.SaveChangesAsync();
}
