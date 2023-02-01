using API.Models;

namespace API.Services
{
    public interface IUserService
    {
        Task Insert(User user);
        Task<User?> GetUserById(Guid userId);
        User? GetUserByProviderId(string providerId);
        
    }
}
