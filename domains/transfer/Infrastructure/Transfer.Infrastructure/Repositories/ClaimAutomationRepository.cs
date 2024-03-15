using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Transfer.Domain.Entities;
using Transfer.Application.Repositories;

namespace DataContext.Repositories;

public class ClaimAutomationRepository : IClaimAutomationRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;

    public ClaimAutomationRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.ClaimAutomationArguments.ToListAsync();
    }

    public async Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.ClaimAutomationArguments.Where(c => c.SubjectId == subject).FirstOrDefaultAsync();
    }

    public async Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.ClaimAutomationArguments.AddAsync(claimAutomationArgument);
        await context.SaveChangesAsync();

        return claimAutomationArgument;
    }

    public async Task DeleteClaimAutomationArgument(ClaimAutomationArgument claim)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        context.ClaimAutomationArguments.Remove(claim);
        await context.SaveChangesAsync();
    }
}
