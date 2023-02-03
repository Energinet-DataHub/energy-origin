using API.Models;

namespace API.Repositories
{
    public interface IUserRepository
    {
        Task<int> UpsertUserAsync(User user);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByProviderIdAsync(string providerId);
    }
}
