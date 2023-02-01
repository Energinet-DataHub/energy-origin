using API.Models;
using Marten;

namespace API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDocumentSession session;

        public UserRepository(IDocumentSession session)
        {
            this.session = session;
        }
        public Task Insert(User user)
        {
            session.Insert(user);
            return session.SaveChangesAsync();
        }

        public async Task<User?> GetById(Guid id) => await session.LoadAsync<User>(id);
        public  User? GetUserByProviderId(string providerId) => session.Query<User>().FirstOrDefault(x => x.ProviderId == providerId);
    }   
}
