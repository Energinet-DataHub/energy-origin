using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data.Interfaces;

public interface IRoleDataContext: IBaseDataContext
{
    DbSet<Role> Roles { get; set; }
}
