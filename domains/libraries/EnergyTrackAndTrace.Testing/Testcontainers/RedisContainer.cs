using System.Threading.Tasks;
using EnergyOrigin.Setup.Cache;
using Testcontainers.Redis;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class RedisContainer : IAsyncLifetime
{
    public global::Testcontainers.Redis.RedisContainer RedisTestContainer { get; }

    public RedisOptions RedisOptions { get; private set; } = default!;

    public RedisContainer()
    {
        RedisTestContainer = new RedisBuilder()
            .WithImage("redis:7.0")
            .WithPortBinding(6379, assignRandomHostPort: true)
            .WithCleanUp(true)
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await RedisTestContainer.StartAsync();

        var connectionString = RedisTestContainer.GetConnectionString();
        RedisOptions = new RedisOptions { ConnectionString = connectionString };
    }

    public async ValueTask DisposeAsync()
    {
        await RedisTestContainer.DisposeAsync();
    }
}
