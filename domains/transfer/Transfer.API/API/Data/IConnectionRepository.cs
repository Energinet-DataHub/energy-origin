using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IConnectionRepository
{
    Task AddConnectionAndDeleteInvitation(Connection newConnection, Guid invitationId);
    Task<List<Connection>> GetCompanyConnections(Guid companyId);
    Task<Connection?> GetConnection(Guid id);
    Task DeleteConnection(Guid id);
    Task<bool> HasConflict(Guid currentCompanyId, Guid senderCompanyId);
}
