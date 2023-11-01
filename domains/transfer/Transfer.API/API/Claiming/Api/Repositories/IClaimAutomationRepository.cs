using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Claiming.Api.Models;

namespace API.Claiming.Api.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimSubject>> GetClaimSubjects();
    Task<ClaimSubject?> GetClaimSubject(Guid subject);

    Task<ClaimSubject> AddClaimSubject(ClaimSubject claimSubject);
    void DeleteClaimSubject(ClaimSubject claim);
}
