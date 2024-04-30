using API.Data;
using API.Models;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Setup;

[Collection(nameof(DatabaseTestCollection))]
public abstract class DatabaseTest : IAsyncLifetime
{
    protected Func<Task> _resetDatabase;
    protected readonly ApplicationDbContext Db;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly Fixture Fixture;
    protected readonly IServiceScope _scope;

    public DatabaseTest(IntegrationTestFactory factory)
    {
        _resetDatabase = factory.ResetDatabase;
        Db = factory.Db;

        _scope = factory.Services.CreateScope();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Fixture = new Fixture();
        Fixture.Customize(new NoCircularReferencesCustomization());
        Fixture.Customize(new IgnoreVirtualMembersCustomization());
    }

    public async Task Insert<T>(T entity) where T : class
    {
        await UnitOfWork.BeginTransactionAsync();

        try
        {
            var repository = Db.Set<T>();

            await repository.AddAsync(entity);

            await UnitOfWork.CommitAsync();
        }
        catch
        {
            await UnitOfWork.RollbackAsync();
            throw;
        }
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
