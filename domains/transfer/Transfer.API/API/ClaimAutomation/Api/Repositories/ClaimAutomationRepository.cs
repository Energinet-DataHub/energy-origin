using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;

namespace API.ClaimAutomation.Api.Repositories;

public class ClaimAutomationRepository : IClaimAutomationRepository
{
    private readonly ApplicationDbContext context;

    public ClaimAutomationRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments()
    {
        return await context.ClaimAutomationArguments.ToListAsync();
    }

    public async Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject)
    {
        return await context.ClaimAutomationArguments.Where(c => c.SubjectId == subject).FirstOrDefaultAsync();
    }

    public async Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument)
    {
        await context.ClaimAutomationArguments.AddAsync(claimAutomationArgument);
        return claimAutomationArgument;
    }

    public void DeleteClaimAutomationArgument(ClaimAutomationArgument claim)
    {
        context.ClaimAutomationArguments.Remove(claim);
    }
}
