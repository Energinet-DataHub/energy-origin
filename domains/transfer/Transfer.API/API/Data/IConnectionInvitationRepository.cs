using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IConnectionInvitationRepository
{
    Task AddConnectionInvitation(ConnectionInvitation connectionInvitation);
}
