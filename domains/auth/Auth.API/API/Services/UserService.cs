using API.Controllers;
using API.Models;
using API.Repositories;

namespace API.Services
{
    public class UserService: IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly ILogger<UserService> logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            this.userRepository = userRepository;
            this.logger = logger;
        }

        public Task Insert(User user)
        {
           return userRepository.Insert(user);
        }

        public async Task<User?> GetUserById(Guid userId)
        {
            try
            {
                return await userRepository.GetUserById(userId);
            }
            catch (Exception e)
            {
                logger.LogError($"GetUserById (UserId = {userId}): {e.Message}");
                throw;
            }
        }

        public async Task<User?> GetUserByProviderId(string providerId)
        {
            try
            {
                return await userRepository.GetUserByProviderId(providerId);
            }
            catch (Exception e)
            {
                logger.LogError($"GetUserByProviderId (ProviderId = {providerId}): {e.Message}");
                throw;
            }
        }
    }
}
