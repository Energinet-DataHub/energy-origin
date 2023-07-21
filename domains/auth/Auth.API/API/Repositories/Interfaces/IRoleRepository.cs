using API.Models.Entities;

namespace API.Repositories.Interfaces;

public interface IRoleRepository
{
    List<Role> GetAllRoles();
    Task<Role?> GetRollByKeyAsync(string key);
    Task<List<Role>> GetRolesWithRoleAdmin();
}
