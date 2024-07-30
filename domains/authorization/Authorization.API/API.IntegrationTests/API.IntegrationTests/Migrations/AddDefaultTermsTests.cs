using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddDefaultTermsMigrationTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddDefaultTermsMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task AddDefaultTerms_Migration_InsertsDefaultTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730104826_AddDefaultTerms");

        var terms = await dbContext.Terms.SingleOrDefaultAsync();
        terms.Should().NotBeNull();
        terms!.Id.Should().Be(Guid.Parse("f41d6fb2-240f-4247-a50b-e4163a1abf98"));
        terms.Version.Should().Be(1);
    }

    [Fact]
    public async Task AddDefaultTerms_Migration_DoesNotInsertDuplicateTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730104826_AddDefaultTerms");

        await migrator.MigrateAsync("20240730104826_AddDefaultTerms");

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(1);
    }
}
