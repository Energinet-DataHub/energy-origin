using Microsoft.EntityFrameworkCore;

namespace Mock.Oidc;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
}