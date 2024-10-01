using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DataContext;
using Xunit;
using EnergyTrackAndTrace.Testing.Testcontainers;

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

    public async Task DisposeAsync()
    {
        foreach (var context in disposableContexts)
        {
            if (context != null)
            {
                await context.DisposeAsync().AsTask();
            }
        }
        await dbContainer.DisposeAsync();
    }
}
