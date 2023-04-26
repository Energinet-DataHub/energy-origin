using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services;

public class UserProviderService : IUserProviderService
{
    private readonly IUserProviderRepository repository;

    public UserProviderService(IUserProviderRepository repository) => this.repository = repository;

    public async Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders) => userProviders.Any() == false ? null : await repository.FindUserProviderMatchAsync(userProviders);

    public List<UserProvider> GetNonMatchingUserProviders(List<UserProvider> newUserProviders, List<UserProvider> oldUserProviders) => newUserProviders
        .ExceptBy(oldUserProviders
            .Select(x => (x.ProviderKeyType, x.UserProviderKey)), x => (x.ProviderKeyType, x.UserProviderKey))
        .ToList();
}
