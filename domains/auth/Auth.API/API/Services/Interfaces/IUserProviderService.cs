using API.Models.Entities;

namespace API.Services.Interfaces;

public interface IUserProviderService
{
    Task<UserProvider> UpsertUserProviderAsync(UserProvider userProvider);
    Task<UserProvider?> GetUserProviderByIdAsync(Guid? userProvider);
    Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders);
    List<UserProvider> GetNonMatchingUserProviders(List<UserProvider> newUserProviders, List<UserProvider> oldUserProviders);
}
