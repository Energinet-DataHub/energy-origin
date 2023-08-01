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
    public async Task<User?> GetUserByIdAsync(Guid id) => await dataContext.Users.Include(x => x.Company).ThenInclude(x=>x!.CompanyTerms).Include(x => x.UserProviders).Include(x=>x.Roles).Include(x=>x.UserTerms).FirstOrDefaultAsync(x => x.Id == id);
    public async Task<User> InsertUserAsync(User user)
    {
        await dataContext.Users.AddAsync(user);
        await dataContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> RemoveUserAsync(User user)
    {
        try
        {
            dataContext.Users.Remove(user);
            await dataContext.SaveChangesAsync();

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!dataContext.Users.Any(x => x.Id == user.Id))
            {
                throw new ArgumentException("User not found.");
            }
            throw;
        }
    }
}
