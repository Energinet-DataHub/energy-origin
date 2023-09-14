using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface IConnectionRepository
{
    Task<List<Connection>> GetCompanyConnections(Guid companyId);
    Task<bool> HasConflict(Guid currentCompanyId, Guid senderCompanyId);
}
