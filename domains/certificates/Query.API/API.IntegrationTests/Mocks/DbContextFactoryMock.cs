using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DataContext;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests.Mocks;

public class DbContextFactoryMock : IDbContextFactory<TransferDbContext>, IAsyncLifetime
{
    private readonly PostgresContainer dbContainer = new();
    private readonly ConcurrentBag<TransferDbContext?> disposableContexts = new();

    public TransferDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TransferDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
        var dbContext = new TransferDbContext(options);
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
