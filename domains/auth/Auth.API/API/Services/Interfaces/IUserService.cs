using API.Models.Entities;
using API.Utilities;

namespace API.Services.Interfaces;

public interface IUserService
{
    Task<User> UpsertUserAsync(User user);
    Task UpdateTermsAccepted(User user, DecodableUserDescriptor descriptor, string traceId);
    Task<User> InsertUserAsync(User user);
    Task<User?> GetUserByIdAsync(Guid? id);
    Task RemoveUserAsync(User user);
    Task<IEnumerable<User>> GetUsersByTinAsync(string tin);
}
