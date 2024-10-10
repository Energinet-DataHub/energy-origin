using System.Data;
using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddOrganizationConsentTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public AddOrganizationConsentTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task AddTerms_Migration_SetsTermsAcceptedToFalseForExistingOrganizations()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var orgId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        await InsertOldOrganization(dbContext, orgId, "12345678", "Test Org");
        await InsertOldClient(dbContext, clientId, "Test Client");
        await InsertOldConsent(dbContext, orgId, clientId);

        var applyMigration = () => migrator.MigrateAsync("20241007174608_AddOrganizationConsentInsertOrganizations");
        await applyMigration.Should().NotThrowAsync();

        var organizations = await dbContext.Organizations.ToListAsync();
        var organizationConsents = await dbContext.OrganizationConsents.ToListAsync();
        var client = await dbContext.Clients.ToListAsync();
        var consents = await dbContext.Consents.ToListAsync();

        organizations.Should().NotBeEmpty();
        // TODO Real asserts
    }

    private static async Task InsertOldOrganization(ApplicationDbContext dbContext, Guid id, string tin, string name)
    {
        var organizationTable = dbContext.Model.FindEntityType(typeof(Organization))!.GetTableName();
        var query = $@"
            INSERT INTO ""{organizationTable}"" (""Id"", ""Tin"", ""Name"", ""TermsAcceptanceDate"", ""TermsAccepted"", ""TermsVersion"")
            VALUES (@Id, @Tin, @Name, @TermsAcceptanceDate, @TermsAccepted, @TermsVersion);";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Tin", tin),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("TermsAcceptanceDate", DBNull.Value),
            new NpgsqlParameter("TermsAccepted", false),
            new NpgsqlParameter("TermsVersion", DBNull.Value));
    }

    private static async Task InsertOldClient(ApplicationDbContext dbContext, Guid id, string name)
    {
        var clientTableName = dbContext.Model.FindEntityType(typeof(Client))!.GetTableName();
        int clientType = 0;
        var query = $@"
            INSERT INTO ""{clientTableName}"" (""Id"", ""IdpClientId"", ""Name"", ""ClientType"", ""RedirectUrl"")
            VALUES (@Id, @IdpClientId, @Name, @ClientType, @RedirectUrl);";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("IdpClientId", id),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("ClientType", clientType),
        new NpgsqlParameter("RedirectUrl", ""));
    }

    private static async Task InsertOldConsent(ApplicationDbContext dbContext, Guid organizationId, Guid clientId)
    {
        var query = $@"
            INSERT INTO ""Consents"" (""OrganizationId"", ""ClientId"", ""ConsentDate"")
            VALUES (@OrganizationId, @ClientId, @ConsentDate);";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("ClientId", clientId),
            new NpgsqlParameter("OrganizationId", organizationId),
            new NpgsqlParameter("ConsentDate", DateTime.Now));
    }
}
