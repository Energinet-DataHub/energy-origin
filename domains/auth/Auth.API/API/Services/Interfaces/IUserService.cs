using API.Models.Entities;

namespace API.Services.Interfaces;

public interface IUserService
{
    Task<User> UpsertUserAsync(User user);
    Task<User> UpdateTermsAccepted(User user);
    Task<User> InsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid? id);
    Task RemoveUserAsync(User user);
    Task<IEnumerable<User>> GetUsersByTinAsync(string tin);
}
