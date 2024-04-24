using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Repository;

public class AffiliationRepository(ApplicationDbContext context) : IAffiliationRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Affiliation?> GetByIdAsync(Guid userId, Guid organizationId)
    {
        return await _context.Affiliations.FindAsync([userId, organizationId]);
    }

    public async Task<IEnumerable<Affiliation>> GetAllAsync()
    {
        return await _context.Affiliations.ToListAsync();
    }

    public async Task AddAsync(Affiliation affiliation)
    {
        ArgumentNullException.ThrowIfNull(affiliation, nameof(affiliation));
        await _context.Affiliations.AddAsync(affiliation);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Affiliation affiliation)
    {
        ArgumentNullException.ThrowIfNull(affiliation, nameof(affiliation));
        _context.Affiliations.Remove(affiliation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Affiliation affiliation)
    {
        ArgumentNullException.ThrowIfNull(affiliation, nameof(affiliation));
        _context.Affiliations.Update(affiliation);
        await _context.SaveChangesAsync();
    }
}
