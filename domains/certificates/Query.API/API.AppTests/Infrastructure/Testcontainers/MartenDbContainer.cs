using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace API.AppTests.Infrastructure.Testcontainers;

public class MartenDbContainer : IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer testContainer;

    public MartenDbContainer() =>
        testContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "marten",
                Username = "postgres",
                Password = "postgres",
            })
            .WithImage("sibedge/postgres-plv8")
            .Build();

    public string ConnectionString => testContainer.ConnectionString;

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
