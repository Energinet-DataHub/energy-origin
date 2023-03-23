using API.Models.Entities;

namespace API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User> UpsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid id);
}
