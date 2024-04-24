using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Repository;

public interface IConsentRepository
{
    Task<Consent?> GetByIdAsync(Guid id);
    Task<IEnumerable<Consent>> GetAllAsync();
    Task AddAsync(Consent consent);
    Task RemoveAsync(Consent consent);
    Task UpdateAsync(Consent consent);
}
