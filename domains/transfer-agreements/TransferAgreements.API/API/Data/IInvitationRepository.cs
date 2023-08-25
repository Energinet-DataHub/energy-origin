using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IInvitationRepository
{
    Task<Invitation> AddInvitationToDb(Invitation invitation);
}
