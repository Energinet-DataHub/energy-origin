using System.Threading.Tasks;
using DataContext;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Shared.Migrations
{
    public class MigrationsTestBase
    {
        protected PostgresContainer container;

        public MigrationsTestBase(IntegrationTestFixture integrationTestFixture)
        {
            container = integrationTestFixture.PostgresContainer;
        }

        protected async Task<ApplicationDbContext> CreateNewCleanDatabase()
        {
            var databaseInfo = await container.CreateNewDatabase();

            var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(databaseInfo.ConnectionString)
                .Options;
            var dbContext = new ApplicationDbContext(contextOptions);
            return dbContext;
        }
    }
}
