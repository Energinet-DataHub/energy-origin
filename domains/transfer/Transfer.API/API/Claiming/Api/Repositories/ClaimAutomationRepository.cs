using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Claiming.Api.Repositories;

public class ClaimAutomationRepository : IClaimAutomationRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;

    public ClaimAutomationRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    async Task<List<ClaimSubject>> IClaimAutomationRepository.GetClaimSubjects()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.ClaimSubjects.ToListAsync();
    }

    public async Task<ClaimSubject?> GetClaimSubject(Guid subject)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.ClaimSubjects.Where(c => c.SubjectId == subject).FirstOrDefaultAsync();
    }

    public async Task<ClaimSubject> AddClaimSubject(ClaimSubject claimSubject)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        await context.ClaimSubjects.AddAsync(claimSubject);
        await context.SaveChangesAsync();

        return claimSubject;
    }

    public async void DeleteClaimSubject(ClaimSubject claim)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        context.ClaimSubjects.Remove(claim);
        await context.SaveChangesAsync();
    }

    public async Task<List<ClaimSubjectHistory>> GetHistory(Guid subject)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.ClaimSubjectHistory.Where(c => c.SubjectId == subject).ToListAsync();
    }
}
