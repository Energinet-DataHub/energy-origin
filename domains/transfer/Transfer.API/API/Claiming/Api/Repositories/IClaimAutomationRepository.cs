using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Claiming.Api.Models;

namespace API.Claiming.Api.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimAutomationArgument>> GetClaimSubjects();
    Task<ClaimAutomationArgument?> GetClaimSubject(Guid subject);

    Task<ClaimAutomationArgument> AddClaimSubject(ClaimAutomationArgument claimAutomationArgument);
    void DeleteClaimSubject(ClaimAutomationArgument claim);
}
