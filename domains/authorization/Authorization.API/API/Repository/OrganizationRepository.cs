using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Repository;

public class OrganizationRepository(ApplicationDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        return await context.Organizations.FindAsync(id);
    }

    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        return await context.Organizations.ToListAsync();
    }

    public async Task AddAsync(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        await context.Organizations.AddAsync(organization);
    }

    public async Task RemoveAsync(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        context.Organizations.Remove(organization);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        context.Organizations.Update(organization);
        await Task.CompletedTask;
    }
}
