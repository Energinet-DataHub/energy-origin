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

        await migrator.MigrateAsync("20240730083845_AddUniqueTermsVersionConstraint");

        var terms1 = Terms.Create(1);
        dbContext.Terms.Add(terms1);
        await dbContext.SaveChangesAsync();

        var terms2 = Terms.Create(1);
        dbContext.Terms.Add(terms2);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(1);
    }

    [Fact]
    public async Task AddUniqueTermsVersionConstraint_Migration_AllowsMultipleTermsWithDifferentVersions()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730083845_AddUniqueTermsVersionConstraint");

        var terms1 = Terms.Create(1);
        var terms2 = Terms.Create(2);
        var terms3 = Terms.Create(3);

        dbContext.Terms.AddRange(terms1, terms2, terms3);

        await dbContext.SaveChangesAsync();

        var termsCount = await dbContext.Terms.CountAsync();
        termsCount.Should().Be(3);
    }
}
