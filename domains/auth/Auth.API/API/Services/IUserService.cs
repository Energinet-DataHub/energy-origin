using API.Models.Entities;

namespace API.Services;

public interface IUserService
{
    Task<User> UpsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid? userId);
    Task<User?> GetUserByProviderIdAsync(string? providerId);
}
