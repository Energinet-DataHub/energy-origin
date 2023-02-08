using API.Models;
using API.Repositories.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IUserDataContext dataContext;

    public UserRepository(IUserDataContext dataContext) => this.dataContext = dataContext;

    public async Task<User> UpsertUserAsync(User user)
    {
        dataContext.Users.Update(user);
        await dataContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid id) => await dataContext.Users.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<User?> GetUserByProviderIdAsync(string providerId) => await dataContext.Users.FirstOrDefaultAsync(x => x.ProviderId == providerId);
}
