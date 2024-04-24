using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Repository;

public class ConsentRepository(ApplicationDbContext context) : IConsentRepository
{
    public async Task<Consent?> GetByIdAsync(Guid id)
    {
        return await context.Consents.FindAsync(id);
    }

    public async Task<IEnumerable<Consent>> GetAllAsync()
    {
        return await context.Consents.ToListAsync();
    }

    public async Task AddAsync(Consent consent)
    {
        ArgumentNullException.ThrowIfNull(consent);

        await context.Consents.AddAsync(consent);
    }

    public async Task RemoveAsync(Consent consent)
    {
        ArgumentNullException.ThrowIfNull(consent);

        context.Consents.Remove(consent);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Consent consent)
    {
        ArgumentNullException.ThrowIfNull(consent);

        context.Consents.Update(consent);
        await Task.CompletedTask;
    }
}
