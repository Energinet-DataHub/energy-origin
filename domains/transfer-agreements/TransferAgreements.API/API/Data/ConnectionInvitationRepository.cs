using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public class ConnectionInvitationRepository : IConnectionInvitationRepository
{
    private readonly ApplicationDbContext context;

    public ConnectionInvitationRepository(ApplicationDbContext context) => this.context = context;

    public async Task<ConnectionInvitation> AddConnectionInvitation(ConnectionInvitation connectionInvitation)
    {
        context.ConnectionInvitations.Add(connectionInvitation);
        await context.SaveChangesAsync();
        return connectionInvitation;
    }
}
