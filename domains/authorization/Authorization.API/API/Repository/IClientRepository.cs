using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repository;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<IEnumerable<Client>> GetAllAsync();
    Task AddAsync(Client client);
    Task RemoveAsync(Client client);
    Task UpdateAsync(Client client);
}
