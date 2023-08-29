using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IInvitationRepository
{
    Task<Invitation> AddInvitation(Invitation invitation);
}
