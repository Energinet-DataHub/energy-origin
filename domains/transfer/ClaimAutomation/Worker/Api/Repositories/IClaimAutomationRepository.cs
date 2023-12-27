using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClaimAutomation.Worker.Api.Models;

namespace ClaimAutomation.Worker.Api.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments();
    Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject);

    Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument);
    Task DeleteClaimAutomationArgument(ClaimAutomationArgument claim);
}
