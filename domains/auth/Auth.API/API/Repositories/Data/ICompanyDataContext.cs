using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data;

public interface ICompanyDataContext : IBaseDataContext
{
    DbSet<Company> Companies { get; set; }
}
