using System;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IConnectionInvitationRepository
{
    Task AddConnectionInvitation(ConnectionInvitation connectionInvitation);
    Task DeleteConnectionInvitation(Guid id);
    Task DeleteOldConnectionInvitations(DateTimeOffset olderThan);
    Task<ConnectionInvitation?> GetNonExpiredConnectionInvitation(Guid id);
}
