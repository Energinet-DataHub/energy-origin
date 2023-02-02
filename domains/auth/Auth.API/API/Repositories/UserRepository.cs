using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext dataContext;

        public UserRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public Task Insert(User user)
        {
            dataContext.Users.Add(user);
            return dataContext.SaveChangesAsync();
        }

        public async Task<User?> GetUserById(Guid id)
        {
            return await dataContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetUserByProviderId(string providerId)
        {
            return await dataContext.Users.FirstOrDefaultAsync(x => x.ProviderId == providerId);
        }
    }   
}
