using API.Models;
using API.Repositories;

namespace API.Services
{
    public class UserService: IUserService
    {
        private readonly IUserRepository userRepository;
        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }
        public Task Insert(User user)
        {
           return userRepository.Insert(user);
        }
        public async Task<User?> GetUserById(Guid userId) => await userRepository.GetById(userId);
        public User? GetUserByProviderId(string providerId) => userRepository.GetUserByProviderId(providerId); 
    }
}
