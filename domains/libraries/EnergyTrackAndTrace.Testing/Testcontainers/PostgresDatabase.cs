using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class PostgresDatabase : IAsyncLifetime
{
    private static PostgresContainer? _postgresContainerInstance;
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    public async Task InitializeAsync()
    {
        if (_postgresContainerInstance is null)
        {
            await Semaphore.WaitAsync();
            try
            {
                if (_postgresContainerInstance is null)
                {
                    _postgresContainerInstance = new PostgresContainer();
                    await _postgresContainerInstance.InitializeAsync();
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<DatabaseInfo> CreateNewDatabase()
    {
        try
        {
            await Semaphore.WaitAsync();
            return await _postgresContainerInstance!.CreateNewDatabase();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    // ~PostgresDatabase()
    // {
    //     if (_postgresContainerInstance is not null)
    //     {
    //         _postgresContainerInstance.DisposeAsync();
    //     }
    // }
}
