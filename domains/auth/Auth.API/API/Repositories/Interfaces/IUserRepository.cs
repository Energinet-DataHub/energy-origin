using API.Models.Entities;

namespace API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User> InsertUserAsync(User user);
    Task<User> UpsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid id);
    Task RemoveUserAsync(User user);
    Task<IEnumerable<User>> GetUsersByTinAsync(string tin);
    void UpdateTermsAccepted(User user);
    Task SaveChangeAsync();
}
