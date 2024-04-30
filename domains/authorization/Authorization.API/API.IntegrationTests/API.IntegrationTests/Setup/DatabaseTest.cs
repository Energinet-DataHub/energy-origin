using API.Data;
using API.Models;
using API.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Setup;

[Collection(nameof(DatabaseTestCollection))]
public abstract class DatabaseTest : IAsyncLifetime
{
    protected Func<Task> _resetDatabase;
    protected readonly ApplicationDbContext Db;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IServiceScope _scope;
    protected readonly IOrganizationRepository OrganizationRepository;

    public DatabaseTest(IntegrationTestFactory factory)
    {
        _resetDatabase = factory.ResetDatabase;
        Db = factory.Db;

        _scope = factory.Services.CreateScope();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        OrganizationRepository = _scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
        if (_scope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _scope.Dispose();
        }
    }
}
