using API.Models;

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
            return dataContext.Users.FirstOrDefault(x => x.Id == id);
        }

        public User? GetUserByProviderId(string providerId)
        {
            return dataContext.Users.FirstOrDefault(x => x.ProviderId == providerId);
        }
    }   
}
