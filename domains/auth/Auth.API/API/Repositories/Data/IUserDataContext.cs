using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data
{
    public interface IUserDataContext : IBaseDataContext
    {
        DbSet<User> Users { get; set; }
    }
}
