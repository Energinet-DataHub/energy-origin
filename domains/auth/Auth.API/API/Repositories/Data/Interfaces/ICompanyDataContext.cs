using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data.Interfaces;

public interface ICompanyDataContext : IBaseDataContext
{
    DbSet<Company> Companies { get; set; }
}
