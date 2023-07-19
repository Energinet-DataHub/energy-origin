using API.Models.Entities;

namespace API.Services.Interfaces;

public interface IRoleService
{
    List<Role> GetAllRoles();
}
