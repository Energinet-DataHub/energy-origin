using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Claiming.Api.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ApplicationDbContext context;

    public ClaimRepository(ApplicationDbContext context)
    {
        this.context = context;
    }


    Task<List<ClaimSubject>> IClaimRepository.GetClaimSubjects()
    {
        return context.ClaimSubjects.ToListAsync();
    }

    public async Task<ClaimSubject?> GetClaimSubject(string subject)
    {
        return await context.ClaimSubjects.Where(c => c.SubjectId.ToString() == subject).FirstOrDefaultAsync();
    }

    public async Task<ClaimSubject> AddClaimSubject(ClaimSubject claimSubject)
    {
        await context.ClaimSubjects.AddAsync(claimSubject);
        await context.SaveChangesAsync();

        return claimSubject;
    }

    public void DeleteClaimSubject(ClaimSubject claim)
    {
        context.ClaimSubjects.Remove(claim);
        context.SaveChanges();
    }
}
