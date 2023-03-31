using API.Models.Entities;

namespace API.Repositories.Interfaces;
public interface IUserProviderRepository
{
    Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders);
}
