using API.Models.Entities;

namespace API.Repositories.Interfaces;
public interface IUserProviderRepository
{
    Task<UserProvider> UpsertUserProviderAsync(UserProvider userProvider);
    Task<UserProvider?> GetUserProviderByIdAsync(Guid id);
    Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders);
}
