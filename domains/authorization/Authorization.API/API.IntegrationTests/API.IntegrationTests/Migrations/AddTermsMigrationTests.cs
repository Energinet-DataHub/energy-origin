using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddTermsMigrationTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public AddTermsMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task AddTerms_Migration_SetsTermsAcceptedToFalseForExistingOrganizations()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240620115450_AddOrganizationTinUniqueIndex");

        await InsertOldOrganization(dbContext, Guid.NewGuid(), "12345678", "Test Org");

        var applyMigration = () => migrator.MigrateAsync("20240703143706_AddTerms");
        await applyMigration.Should().NotThrowAsync();

        var organizations = await dbContext.Organizations.ToListAsync();
        organizations.Should().NotBeEmpty();
        organizations.Should().AllSatisfy(org =>
        {
            org.TermsAccepted.Should().BeFalse();
            org.TermsVersion.Should().BeNull();
            org.TermsAcceptanceDate.Should().BeNull();
        });
    }

    private static async Task InsertOldOrganization(ApplicationDbContext dbContext, Guid id, string tin, string name)
    {
        var organizationTable = dbContext.Model.FindEntityType(typeof(Organization))!.GetTableName();

        var query = $@"
            INSERT INTO ""{organizationTable}"" (""Id"", ""Tin"", ""Name"")
            VALUES (@Id, @Tin, @Name)";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Tin", tin),
            new NpgsqlParameter("Name", name));
    }
}
