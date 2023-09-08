using System.Collections.Concurrent;
using System.Threading.Tasks;
using API.Data;
using API.IntegrationTests.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Mocks;

public class DbContextFactoryMock : IDbContextFactory<ApplicationDbContext>, IAsyncLifetime
{
    private readonly PostgresContainer dbContainer = new();
    private readonly ConcurrentBag<ApplicationDbContext?> disposableContexts = new();

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        disposableContexts.Add(dbContext);
        return dbContext;
    }

    public Task InitializeAsync()
        => dbContainer.InitializeAsync();

    public Task DisposeAsync()
    {
        foreach (var context in disposableContexts)
        {
            context?.DisposeAsync();
        }
        return dbContainer.DisposeAsync();
    }
}
