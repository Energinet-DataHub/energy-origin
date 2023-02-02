using API.Models;

namespace API.Repositories
{
    public interface IUserRepository
    {
        Task Insert(User user);
        Task<User?> GetUserById(Guid id);
        User? GetUserByProviderId(string providerId);
    }
}
