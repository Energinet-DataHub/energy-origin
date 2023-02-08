using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data
{
    public interface IUserDataContext : IBaseDataContext
    {
        DbSet<User> Users { get; set; }
    }
}
