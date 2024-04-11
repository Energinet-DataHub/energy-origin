using API.IntegrationTests.Testcontainers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using DataContext;

namespace API.IntegrationTests.Shared.Migrations
{
    public class MigrationsTestBase : IAsyncDisposable
    {
        protected PostgresContainer container;
        public MigrationsTestBase()
        {
            container = new PostgresContainer();
        }

        protected async Task<TransferDbContext> CreateNewCleanDatabase()
        {
            await container.InitializeAsync();

            var contextOptions = new DbContextOptionsBuilder<TransferDbContext>().UseNpgsql(container.ConnectionString)
                .Options;
            var dbContext = new TransferDbContext(contextOptions);
            return dbContext;
        }

        public async ValueTask DisposeAsync()
        {
            await container.DisposeAsync();
        }
    }
}
