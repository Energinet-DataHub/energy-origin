using API.Models.Entities;

namespace API.Repositories;

public interface IUserRepository
{
    Task<User> UpsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserByProviderIdAsync(string providerId);
}
