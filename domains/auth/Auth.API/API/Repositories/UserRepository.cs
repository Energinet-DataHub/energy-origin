using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
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
    public async Task<User?> GetUserByIdAsync(Guid id) => await dataContext.Users.Include(x => x.Company).Include(x => x.UserProviders).FirstOrDefaultAsync(x => x.Id == id);
}
