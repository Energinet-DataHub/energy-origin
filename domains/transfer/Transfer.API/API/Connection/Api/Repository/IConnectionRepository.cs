using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Connection.Api.Repository;

public interface IConnectionRepository
{
    Task AddConnectionAndDeleteInvitation(Models.Connection newConnection, Guid invitationId);
    Task<List<Models.Connection>> GetCompanyConnections(Guid companyId);
    Task<Models.Connection?> GetConnection(Guid id);
    Task DeleteConnection(Guid id);
    Task<bool> HasConflict(Guid currentCompanyId, Guid senderCompanyId);
}
