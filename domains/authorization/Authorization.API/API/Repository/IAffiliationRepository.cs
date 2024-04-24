using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repository;

public interface IAffiliationRepository
{
    Task<Affiliation?> GetByIdAsync(Guid userId, Guid organizationId);
    Task<IEnumerable<Affiliation>> GetAllAsync();
    Task AddAsync(Affiliation affiliation);
    Task RemoveAsync(Affiliation affiliation);
    Task UpdateAsync(Affiliation affiliation);
}
