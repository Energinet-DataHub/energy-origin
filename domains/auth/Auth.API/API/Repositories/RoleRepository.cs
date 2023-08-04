using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IRoleDataContext dataContext;

    public RoleRepository(IRoleDataContext dataContext) => this.dataContext = dataContext;

    public List<Role> GetAllRoles() => dataContext.Roles.ToList();
    public async Task<Role?> GetRollByKeyAsync(string key) => await dataContext.Roles.FirstOrDefaultAsync(x => x.Key == key);
}
