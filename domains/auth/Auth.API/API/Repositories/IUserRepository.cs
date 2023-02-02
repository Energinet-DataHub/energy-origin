using API.Models;

namespace API.Repositories
{
    public interface IUserRepository
    {
        Task Insert(User user);
        Task<User?> GetUserById(Guid id);
        Task<User?> GetUserByProviderId(string providerId);
    }
}
