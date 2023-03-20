using API.Models.Entities;
using API.Repositories;

namespace API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository userRepository;

    public UserService(IUserRepository userRepository) => this.userRepository = userRepository;

    public async Task<User> UpsertUserAsync(User user) => await userRepository.UpsertUserAsync(user);
    public async Task<User?> GetUserByIdAsync(Guid? userId) => userId is null ? null : await userRepository.GetUserByIdAsync(userId.Value);
    public async Task<User?> GetUserByProviderIdAsync(string? providerId) => providerId is null ? null : await userRepository.GetUserByProviderIdAsync(providerId);
}
