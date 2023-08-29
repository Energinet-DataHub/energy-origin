using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;

namespace API.Data;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext context;

    public ConnectionRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public List<Connection> GetCompanyConnections(Guid companyId) => context.Connections.Where(x => x.CompanyAId == companyId || x.CompanyBId == companyId).ToList();
}
