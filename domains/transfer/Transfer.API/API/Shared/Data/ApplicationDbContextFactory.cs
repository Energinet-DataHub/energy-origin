using Microsoft.EntityFrameworkCore;
using API.Shared.Options;
using Microsoft.Extensions.Options;

namespace API.Shared.Data;

public class ApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DatabaseOptions databaseOptions;

    public ApplicationDbContextFactory(IOptions<DatabaseOptions> options)
    {
        databaseOptions = options.Value;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(databaseOptions.ToConnectionString());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
