using System.ComponentModel.Design;
using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services;

public class UserProviderService : IUserProviderService
{
    private readonly IUserProviderRepository repository;

    public UserProviderService(IUserProviderRepository repository) => this.repository = repository;

    public async Task<UserProvider> UpsertUserProviderAsync(UserProvider userProvider) => await repository.UpsertUserProviderAsync(userProvider);
    public async Task<UserProvider?> GetUserProviderByIdAsync(Guid? id) => id is null ? null : await repository.GetUserProviderByIdAsync(id.Value);
    public async Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders) => userProviders.Any() == false ? null : await repository.FindUserProviderMatchAsync(userProviders);
    public List<UserProvider> GetNonMatchingUserProviders(List<UserProvider> newUserProviders, List<UserProvider> oldUserProviders) =>
        newUserProviders
            .ExceptBy(oldUserProviders
                .Select(x => (x.ProviderKeyType, x.UserProviderKey, x.ProviderType)), x => (x.ProviderKeyType, x.UserProviderKey, x.ProviderType))
            .ToList();
}
