using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data.Interfaces;

public interface IUserProviderDataContext : IBaseDataContext
{
    DbSet<UserProvider> UserProviders { get; set; }
}
