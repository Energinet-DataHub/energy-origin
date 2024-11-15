using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddDefaultServiceProviderTermsWithUniqueConstraintTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddDefaultServiceProviderTermsWithUniqueConstraintTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(newDatabaseInfo.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task AddUniqueServiceProviderTermsVersionConstraint_Migration_EnforcesUniqueVersionConstraint()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241115124556_AddServiceProviderTerms");

        await migrator.MigrateAsync("20241115125358_AddDefaultServiceProviderTermsWithUniqueConstraint");

        var terms2 = ServiceProviderTerms.Create(2);
        dbContext.ServiceProviderTerms.Add(terms2);
        await dbContext.SaveChangesAsync();

        var duplicateTerms2 = ServiceProviderTerms.Create(2);
        dbContext.ServiceProviderTerms.Add(duplicateTerms2);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        var termsCount = await dbContext.ServiceProviderTerms.CountAsync();
        termsCount.Should().Be(2); // Default terms (version 1) + terms2
    }

    [Fact]
    public async Task AddUniqueServiceProviderTermsVersionConstraint_Migration_AllowsMultipleTermsWithDifferentVersions()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241115124556_AddServiceProviderTerms");

        await migrator.MigrateAsync("20241115125358_AddDefaultServiceProviderTermsWithUniqueConstraint");

        var terms2 = ServiceProviderTerms.Create(2);
        var terms3 = ServiceProviderTerms.Create(3);
        var terms4 = ServiceProviderTerms.Create(4);

        dbContext.ServiceProviderTerms.AddRange(terms2, terms3, terms4);

        await dbContext.SaveChangesAsync();

        var termsCount = await dbContext.ServiceProviderTerms.CountAsync();
        termsCount.Should().Be(4);
    }

    [Fact]
    public async Task AddDefaultServiceProviderTerms_Migration_InsertsDefaultTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241115124556_AddServiceProviderTerms");

        await migrator.MigrateAsync("20241115125358_AddDefaultServiceProviderTermsWithUniqueConstraint");

        var terms = await dbContext.ServiceProviderTerms.SingleOrDefaultAsync();
        terms.Should().NotBeNull();
        terms!.Id.Should().Be(Guid.Parse("a545358f-0475-43b4-a911-6fa8009ec0da"));
        terms.Version.Should().Be(1);
    }

    [Fact]
    public async Task AddDefaultServiceProviderTerms_Migration_DoesNotInsertDuplicateTerms()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241115124556_AddServiceProviderTerms");

        await migrator.MigrateAsync("20241115125358_AddDefaultServiceProviderTermsWithUniqueConstraint");
        await migrator.MigrateAsync("20241115125358_AddDefaultServiceProviderTermsWithUniqueConstraint");

        var termsCount = await dbContext.ServiceProviderTerms.CountAsync();
        termsCount.Should().Be(1);
    }
}
