using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class ChangeServiceProviderTermsToAcceptedForClientOrganizationsTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ChangeServiceProviderTermsToAcceptedForClientOrganizationsTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(newDatabaseInfo.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task ChangeServiceProviderTerms_Migration_UpdatesServiceProviderTermsForOrganizationsAssociatedWithClients()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241120144111_AddServiceProviderTermsFieldsToOrganizationsTable");

        var orgIdWithClient = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var orgIdWithoutClient = Guid.NewGuid();

        await InsertOrganization(dbContext, orgIdWithClient, "12345678", "Org With Client", serviceProviderTermsAccepted: false);

        await InsertClient(dbContext, clientId, "Test Client", orgIdWithClient);

        await InsertOrganization(dbContext, orgIdWithoutClient, "87654321", "Org Without Client", serviceProviderTermsAccepted: false);

        var applyMigration = () => migrator.MigrateAsync("20241125102638_ChangeServiceProviderTermsToAcceptedForClientOrganizations");
        await applyMigration.Should().NotThrowAsync();

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var commandText = @"
            SELECT ""Id"", ""ServiceProviderTermsAccepted"", ""ServiceProviderTermsAcceptanceDate""
            FROM ""Organizations""
            WHERE ""Id"" = @OrgIdWithClient OR ""Id"" = @OrgIdWithoutClient;";

        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.AddWithValue("OrgIdWithClient", orgIdWithClient);
        command.Parameters.AddWithValue("OrgIdWithoutClient", orgIdWithoutClient);

        var organizations = new List<(Guid Id, bool ServiceProviderTermsAccepted, DateTime? ServiceProviderTermsAcceptanceDate)>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetGuid(reader.GetOrdinal("Id"));
            var serviceProviderTermsAccepted = reader.GetBoolean(reader.GetOrdinal("ServiceProviderTermsAccepted"));
            var acceptanceDateOrdinal = reader.GetOrdinal("ServiceProviderTermsAcceptanceDate");
            DateTime? acceptanceDate = null;
            if (!reader.IsDBNull(acceptanceDateOrdinal))
            {
                acceptanceDate = reader.GetFieldValue<DateTime>(acceptanceDateOrdinal);
            }
            organizations.Add((id, serviceProviderTermsAccepted, acceptanceDate));
        }

        // Organization associated with the client should have accepted the service provider terms
        var orgWithClient = organizations.First(o => o.Id == orgIdWithClient);
        orgWithClient.ServiceProviderTermsAccepted.Should().BeTrue();
        orgWithClient.ServiceProviderTermsAcceptanceDate.Should().NotBeNull();

        // Organization not associated with any client should not have accepted the service provider terms
        var orgWithoutClient = organizations.First(o => o.Id == orgIdWithoutClient);
        orgWithoutClient.ServiceProviderTermsAccepted.Should().BeFalse();
        orgWithoutClient.ServiceProviderTermsAcceptanceDate.Should().BeNull();
    }

    private static async Task InsertOrganization(ApplicationDbContext dbContext, Guid id, string tin, string name, bool serviceProviderTermsAccepted)
    {
        var organizationTable = dbContext.Model.FindEntityType(typeof(Organization))!.GetTableName();
        var query = $@"
            INSERT INTO ""{organizationTable}"" (
                ""Id"",
                ""Tin"",
                ""Name"",
                ""TermsAcceptanceDate"",
                ""TermsAccepted"",
                ""TermsVersion"",
                ""ServiceProviderTermsAccepted"",
                ""ServiceProviderTermsAcceptanceDate""
            )
            VALUES (
                @Id,
                @Tin,
                @Name,
                @TermsAcceptanceDate,
                @TermsAccepted,
                @TermsVersion,
                @ServiceProviderTermsAccepted,
                @ServiceProviderTermsAcceptanceDate
            );";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Tin", (object)tin ?? DBNull.Value),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("TermsAcceptanceDate", DBNull.Value),
            new NpgsqlParameter("TermsAccepted", false),
            new NpgsqlParameter("TermsVersion", DBNull.Value),
            new NpgsqlParameter("ServiceProviderTermsAccepted", serviceProviderTermsAccepted),
            new NpgsqlParameter("ServiceProviderTermsAcceptanceDate", DBNull.Value));
    }

    private static async Task InsertClient(ApplicationDbContext dbContext, Guid id, string name, Guid organizationId)
    {
        var clientTableName = dbContext.Model.FindEntityType(typeof(Client))!.GetTableName();
        int clientType = 0;
        var query = $@"
            INSERT INTO ""{clientTableName}"" (
                ""Id"",
                ""IdpClientId"",
                ""Name"",
                ""ClientType"",
                ""RedirectUrl"",
                ""OrganizationId""
            )
            VALUES (
                @Id,
                @IdpClientId,
                @Name,
                @ClientType,
                @RedirectUrl,
                @OrganizationId
            );";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("IdpClientId", id),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("ClientType", clientType),
            new NpgsqlParameter("RedirectUrl", ""),
            new NpgsqlParameter("OrganizationId", organizationId));
    }
}
