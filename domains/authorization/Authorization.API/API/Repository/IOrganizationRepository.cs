using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repository;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id);
    Task<IEnumerable<Organization>> GetAllAsync();
    Task AddAsync(Organization organization);
    Task RemoveAsync(Organization organization);
    Task UpdateAsync(Organization organization);
}
