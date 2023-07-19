using API.Models.Entities;

namespace API.Repositories.Interfaces;

public interface IRoleRepository
{
    List<Role> GetAllRoles();
}
