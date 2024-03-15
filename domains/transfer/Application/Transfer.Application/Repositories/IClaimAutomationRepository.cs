using Transfer.Domain.Entities;

namespace Transfer.Application.Repositories;

public interface IClaimAutomationRepository
{
    Task<List<ClaimAutomationArgument>> GetClaimAutomationArguments();
    Task<ClaimAutomationArgument?> GetClaimAutomationArgument(Guid subject);

    Task<ClaimAutomationArgument> AddClaimAutomationArgument(ClaimAutomationArgument claimAutomationArgument);
    Task DeleteClaimAutomationArgument(ClaimAutomationArgument claim);
}
