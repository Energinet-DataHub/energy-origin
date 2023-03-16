using API.Models.Entities;
using API.Repositories;

namespace API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository userRepository;

    public UserService(IUserRepository userRepository) => this.userRepository = userRepository;

    public async Task<User> UpsertUserAsync(User user) => await userRepository.UpsertUserAsync(user);
    public async Task<User?> GetUserByIdAsync(Guid userId) => await userRepository.GetUserByIdAsync(userId);
    public async Task<User?> GetUserByProviderIdAsync(string providerId) => await userRepository.GetUserByProviderIdAsync(providerId);
}
