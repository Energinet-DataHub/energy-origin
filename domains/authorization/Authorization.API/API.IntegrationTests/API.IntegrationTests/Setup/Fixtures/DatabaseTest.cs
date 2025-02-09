using API.Models;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Setup.Fixtures;

[Collection(nameof(BaseFixtureForTesting))]
public abstract class DatabaseTest : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private Func<Task> _resetDatabase;
    private readonly IServiceScope _scope;
    protected readonly ApplicationDbContext Db;

    protected DatabaseTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _resetDatabase = factory.ResetDatabase;

        _scope = _factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
