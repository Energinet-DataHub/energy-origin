using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class UserProviderRepository : IUserProviderRepository
{
    private readonly IUserProviderDataContext dataContext;

    public UserProviderRepository(IUserProviderDataContext dataContext) => this.dataContext = dataContext;

    public async Task<UserProvider> UpsertUserProviderAsync(UserProvider userProvider)
    {
        dataContext.UserProviders.Update(userProvider);
        await dataContext.SaveChangesAsync();
        return userProvider;
    }
    public async Task<UserProvider?> GetUserProviderByIdAsync(Guid id) => await dataContext.UserProviders.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<UserProvider?> FindUserProviderMatchAsync(List<UserProvider> userProviders)
    {
        IQueryable<UserProvider>? query = null;
        foreach (var userProvider in userProviders)
        {
            var temp = dataContext.UserProviders.Where(x => x.ProviderKeyType == userProvider.ProviderKeyType && x.UserProviderKey == userProvider.UserProviderKey);

            query = query == null ? temp : query.Union(temp);
        }

        return await query?.FirstOrDefaultAsync()!;
    }
}
