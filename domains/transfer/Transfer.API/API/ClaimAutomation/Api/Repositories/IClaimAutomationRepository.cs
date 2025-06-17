using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.ClaimAutomation.Api.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments();
    Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject);

    Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument);
    void DeleteClaimAutomationArgument(ClaimAutomationArgument claim);
}
