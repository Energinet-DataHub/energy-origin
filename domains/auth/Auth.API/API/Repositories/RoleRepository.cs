using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;

namespace API.Repositories;

public class RoleRepository: IRoleRepository
{
    private readonly IRoleDataContext dataContext;

    public RoleRepository(IRoleDataContext dataContext) => this.dataContext = dataContext;

    public List<Role> GetAllRoles() => dataContext.Roles.ToList();
}
