using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

public class AddOrganizationConsentTests : IntegrationTestBase, IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddOrganizationConsentTests(IntegrationTestFixture fixture) : base(fixture)
    {
        var newDatabaseInfo = _fixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task AddTerms_Migration_SetsTermsAcceptedToFalseForExistingOrganizations()
    {
        // Given old client and organization data
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240730115826_AddDefaultTermsWithUniqueConstraint");

        var orgId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        await InsertOldOrganization(dbContext, orgId, "12345678", "Test Org");
        await InsertOldClient(dbContext, clientId, "Test Client");
        await InsertOldConsent(dbContext, orgId, clientId);

        // When migrating schema
        var applyMigration = () => migrator.MigrateAsync("20241021074018_RemoveConsentTable");
        await applyMigration.Should().NotThrowAsync();

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Query Organizations
        var organizations = new List<(Guid Id, string Name, string? Tin)>();
        var orgCommandText = @"
            SELECT ""Id"", ""Name"", ""Tin""
            FROM ""Organizations""";

        await using (var orgCommand = new NpgsqlCommand(orgCommandText, connection))
        {
            await using var orgReader = await orgCommand.ExecuteReaderAsync();

            while (await orgReader.ReadAsync())
            {
                var id = orgReader.GetGuid(orgReader.GetOrdinal("Id"));
                var name = orgReader.GetString(orgReader.GetOrdinal("Name"));
                var tinOrdinal = orgReader.GetOrdinal("Tin");

                string? tin = null;
                if (!orgReader.IsDBNull(tinOrdinal))
                {
                    tin = orgReader.GetString(tinOrdinal);
                }

                organizations.Add((id, name, tin));
            }
        }

        // Query Clients
        var clients = new List<(Guid Id, string Name, Guid IdpClientId, Guid OrganizationId)>();
        var clientCommandText = @"
            SELECT ""Id"", ""Name"", ""IdpClientId"", ""OrganizationId""
            FROM ""Clients""";

        await using (var clientCommand = new NpgsqlCommand(clientCommandText, connection))
        {
            await using var clientReader = await clientCommand.ExecuteReaderAsync();

            while (await clientReader.ReadAsync())
            {
                var id = clientReader.GetGuid(clientReader.GetOrdinal("Id"));
                var name = clientReader.GetString(clientReader.GetOrdinal("Name"));
                var idpClientId = clientReader.GetGuid(clientReader.GetOrdinal("IdpClientId"));
                var organizationId = clientReader.GetGuid(clientReader.GetOrdinal("OrganizationId"));

                clients.Add((id, name, idpClientId, organizationId));
            }
        }

        // Query OrganizationConsents
        var organizationConsents = new List<(Guid ConsentGiverOrganizationId, Guid ConsentReceiverOrganizationId)>();
        var consentCommandText = @"
            SELECT ""ConsentGiverOrganizationId"", ""ConsentReceiverOrganizationId""
            FROM ""OrganizationConsents""";

        await using (var consentCommand = new NpgsqlCommand(consentCommandText, connection))
        {
            await using var consentReader = await consentCommand.ExecuteReaderAsync();

            while (await consentReader.ReadAsync())
            {
                var consentGiverOrganizationId = consentReader.GetGuid(consentReader.GetOrdinal("ConsentGiverOrganizationId"));
                var consentReceiverOrganizationId = consentReader.GetGuid(consentReader.GetOrdinal("ConsentReceiverOrganizationId"));

                organizationConsents.Add((consentGiverOrganizationId, consentReceiverOrganizationId));
            }
        }

        // Then organization, client and consent should be migrated
        clients.Should().ContainSingle(c => c.Name == "Test Client" && c.IdpClientId == clientId);
        var clientData = clients.First();
        organizations.Should().HaveCount(2);
        organizations.Should().ContainSingle(o => o.Name == "Test Client" && o.Id == clientData.OrganizationId);
        organizations.Should().ContainSingle(o => o.Name == "Test Org" && o.Id == orgId && o.Tin == "12345678");
        organizationConsents.Should().ContainSingle(orgConsent =>
            orgConsent.ConsentGiverOrganizationId == orgId && orgConsent.ConsentReceiverOrganizationId == clientData.OrganizationId);
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
            new NpgsqlParameter("ConsentDate", DateTime.UtcNow));
    }
}
