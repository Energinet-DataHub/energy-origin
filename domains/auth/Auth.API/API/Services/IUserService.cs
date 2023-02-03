using API.Models;

namespace API.Services
{
    public interface IUserService
    {
        Task<int> UpsertUserAsync(User user);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByProviderIdAsync(string providerId);
    }
}
