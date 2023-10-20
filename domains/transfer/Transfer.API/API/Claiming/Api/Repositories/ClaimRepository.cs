using System.Collections.Generic;
using System.Linq;
using API.Claiming.Api.Models;
using API.Shared.Data;

namespace API.Claiming.Api.Repositories;

public interface IClaimRepository
{
    public List<ClaimSubject> GetClaimSubjects();
}

public class ClaimRepository : IClaimRepository
{
    private readonly ApplicationDbContext context;
    public ClaimRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public List<ClaimSubject> GetClaimSubjects()
    {
        return context.ClaimSubjects.ToList();
    }
}
