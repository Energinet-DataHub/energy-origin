using API.Models;

namespace API.Repositories;

public interface IUserRepository
{
    Task<User> UpsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserByProviderIdAsync(string providerId);
}
