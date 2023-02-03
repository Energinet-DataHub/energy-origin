using API.Models;
using API.Repositories.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUserDataContext dataContext;

        public UserRepository(IUserDataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<int> UpsertUserAsync(User user)
        {
            dataContext.Users.Update(user);
            return await dataContext.SaveChangesAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await dataContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetUserByProviderIdAsync(string providerId)
        {
            return await dataContext.Users.FirstOrDefaultAsync(x => x.ProviderId == providerId);
        }
    }
}
