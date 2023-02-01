using API.Models;

namespace API.Repositories
{
    public interface IUserRepository
    {
        Task Insert(User user);
        Task<User?> GetById(Guid id);
        User? GetUserByProviderId(string providerId);
    }
}
