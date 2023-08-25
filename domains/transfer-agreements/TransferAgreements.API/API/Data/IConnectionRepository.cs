using System;
using System.Collections.Generic;
using API.Models;

namespace API.Data
{
    public interface IConnectionRepository
    {
        List<Connection> GetOwnedConnections(Guid ownerId);
    }
}
