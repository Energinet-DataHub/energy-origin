using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository repository;

    public RoleService(IRoleRepository repository) => this.repository = repository;
    public List<Role> GetAllRoles() => repository.GetAllRoles();
    public async Task<Role?> GetRollByKeyAsync(string key) => await repository.GetRollByKeyAsync(key);
}
