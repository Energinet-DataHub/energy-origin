using System;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IConnectionInvitationRepository
{
    Task AddConnectionInvitation(ConnectionInvitation connectionInvitation);
    Task DeleteOldConnectionInvitations(DateTimeOffset olderThan);
    Task<ConnectionInvitation?> FindConnectionInvitation(Guid id);
}
