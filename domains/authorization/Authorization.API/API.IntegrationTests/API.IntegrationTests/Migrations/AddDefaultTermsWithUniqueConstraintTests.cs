using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddUniqueTermsVersionConstraintMigrationTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddUniqueTermsVersionConstraintMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task AddUniqueTermsVersionConstraint_Migration_EnforcesUniqueVersionConstraint()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var terms2 = Terms.Create(2);
        dbContext.Terms.Add(terms2);
        await dbContext.SaveChangesAsync();

        var duplicateTerms2 = Terms.Create(2);
        dbContext.Terms.Add(duplicateTerms2);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(2);
    }

    [Fact]
    public async Task AddUniqueTermsVersionConstraint_Migration_AllowsMultipleTermsWithDifferentVersions()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var terms2 = Terms.Create(2);
        var terms3 = Terms.Create(3);
        var terms4 = Terms.Create(4);

        dbContext.Terms.AddRange(terms2, terms3, terms4);

        await dbContext.SaveChangesAsync();

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(4);
    }

    [Fact]
    public async Task AddDefaultTerms_Migration_InsertsDefaultTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var terms = await dbContext.Terms.SingleOrDefaultAsync();
        terms.Should().NotBeNull();
        terms!.Id.Should().Be(Guid.Parse("0ccb0348-3179-4b96-9be0-dc7ab1541771"));
        terms.Version.Should().Be(1);
    }

    [Fact]
    public async Task AddDefaultTerms_Migration_DoesNotInsertDuplicateTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(1);
    }
}
