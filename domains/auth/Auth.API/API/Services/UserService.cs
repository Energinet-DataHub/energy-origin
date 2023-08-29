using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository repository;

    public UserService(IUserRepository repository) => this.repository = repository;

    public async Task<User> UpsertUserAsync(User user) => await repository.UpsertUserAsync(user);
    public async Task<User?> GetUserByIdAsync(Guid? id) => id is null ? null : await repository.GetUserByIdAsync(id.Value);
    public async Task<User> InsertUserAsync(User user) => await repository.InsertUserAsync(user);
    public async Task RemoveUserAsync(User user) => await repository.RemoveUserAsync(user);
}
