using System;
using System.Threading.Tasks;
using API.Connection.Api.Models;

namespace API.Connection.Api.Repository;

public interface IConnectionInvitationRepository
{
    Task AddConnectionInvitation(ConnectionInvitation connectionInvitation);
    Task DeleteConnectionInvitation(Guid id);
    Task DeleteOldConnectionInvitations(DateTimeOffset olderThan);
    Task<ConnectionInvitation?> GetNonExpiredConnectionInvitation(Guid id);
}
