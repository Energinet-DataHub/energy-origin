using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Claiming.Api.Models;

namespace API.Claiming.Api.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments();
    Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject);

    Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument);
    void DeleteClaimAutomationArgument(ClaimAutomationArgument claim);
}
