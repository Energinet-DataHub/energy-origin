using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.AppTests.Testcontainers;

public class MartenDbContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer;

    public MartenDbContainer() =>
        testContainer = new PostgreSqlBuilder()
            .WithDatabase("marten")
            .WithPassword("postgres")
            .WithUsername("postgres")
            .WithImage("sibedge/postgres-plv8")
            .Build();

    public string ConnectionString => testContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();

        await testContainer.ExecAsync(new[]
        {
            "/bin/sh", "-c",
            "psql -U postgres -c \"CREATE EXTENSION plv8; SELECT extversion FROM pg_extension WHERE extname = 'plv8';\""
        });
    }

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
