using API.IntegrationTests.Testcontainers;
using API.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace API.IntegrationTests.Shared.Migrations
{
    public class MigrationsTestBase : IAsyncDisposable
    {
        protected PostgresContainer container;
        public MigrationsTestBase()
        {
            container = new PostgresContainer();
        }

        protected async Task<ApplicationDbContext> CreateNewCleanDatabase()
        {
            await container.InitializeAsync();

            var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.ConnectionString)
                .Options;
            var dbContext = new ApplicationDbContext(contextOptions);
            return dbContext;
        }

        public async ValueTask DisposeAsync()
        {
            await container.DisposeAsync();
        }
    }
}
