using API.Models;
using API.Repositories;

namespace API.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly ILogger<IUserService> logger;

        public UserService(IUserRepository userRepository, ILogger<IUserService> logger)
        {
            this.userRepository = userRepository;
            this.logger = logger;
        }

        public async Task<User> UpsertUserAsync(User user) => await userRepository.UpsertUserAsync(user);

        public async Task<User?> GetUserByIdAsync(Guid userId) => await userRepository.GetUserByIdAsync(userId);

        public async Task<User?> GetUserByProviderIdAsync(string providerId) => await userRepository.GetUserByProviderIdAsync(providerId);
    }
}
