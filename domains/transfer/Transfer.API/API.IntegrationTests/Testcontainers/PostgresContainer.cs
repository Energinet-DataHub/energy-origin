using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class PostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer;

    public PostgresContainer() => testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    public string ConnectionString => testContainer.GetConnectionString();

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
