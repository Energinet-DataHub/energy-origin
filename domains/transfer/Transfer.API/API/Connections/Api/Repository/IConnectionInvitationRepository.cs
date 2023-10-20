using System;
using System.Threading.Tasks;
using API.Connections.Api.Models;

namespace API.Connections.Api.Repository;

public interface IConnectionInvitationRepository
{
    Task AddConnectionInvitation(ConnectionInvitation connectionInvitation);
    Task DeleteConnectionInvitation(Guid id);
    Task DeleteOldConnectionInvitations(DateTimeOffset olderThan);
    Task<ConnectionInvitation?> GetNonExpiredConnectionInvitation(Guid id);
}
