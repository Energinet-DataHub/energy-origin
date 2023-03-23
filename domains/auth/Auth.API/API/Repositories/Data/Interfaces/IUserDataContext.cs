using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data.Interfaces;

public interface IUserDataContext : IBaseDataContext
{
    DbSet<User> Users { get; set; }
}
