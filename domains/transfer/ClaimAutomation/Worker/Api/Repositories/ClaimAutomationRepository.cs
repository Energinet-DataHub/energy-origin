using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimAutomation.Worker.Api.Repositories;

public class ClaimAutomationRepository : IClaimAutomationRepository
{
    private readonly IDbContextFactory<TransferDbContext> contextFactory;

    public ClaimAutomationRepository(IDbContextFactory<TransferDbContext> contextFactory)
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
