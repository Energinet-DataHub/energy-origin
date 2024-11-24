using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

public class AddServiceProviderTermsTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddServiceProviderTermsTests(IntegrationTestFixture fixture) : base(fixture)
    {
        var newDatabaseInfo = _fixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task ApplyingMigration_AddsServiceProviderTermsFieldsToExistingOrganization()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241022085628_AddUniqueConsentIndex");

        var orgId = Guid.NewGuid();
        await InsertTestOrganization(dbContext, orgId, "12345678", "Test Organization");

        await migrator.MigrateAsync("20241120144111_AddServiceProviderTermsFieldsToOrganizationsTable");

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var commandText = @"
                SELECT ""Id"", ""ServiceProviderTermsAccepted"", ""ServiceProviderTermsAcceptanceDate""
                FROM ""Organizations""
                WHERE ""Id"" = @Id;";

        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.AddWithValue("Id", orgId);

        await using var reader = await command.ExecuteReaderAsync();

        bool organizationFound = false;

        while (await reader.ReadAsync())
        {
            organizationFound = true;

            var serviceProviderTermsAccepted = reader.GetBoolean(reader.GetOrdinal("ServiceProviderTermsAccepted"));

            DateTimeOffset? serviceProviderTermsAcceptanceDate = null;
            if (!reader.IsDBNull(reader.GetOrdinal("ServiceProviderTermsAcceptanceDate")))
            {
                serviceProviderTermsAcceptanceDate = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ServiceProviderTermsAcceptanceDate"));
            }

            serviceProviderTermsAccepted.Should().BeFalse();
            serviceProviderTermsAcceptanceDate.Should().BeNull();
        }

        organizationFound.Should().BeTrue("Organization should exist in the database.");
    }

    private static async Task InsertTestOrganization(ApplicationDbContext dbContext, Guid id, string tin, string name)
    {
        var organizationTable = dbContext.Model.FindEntityType(typeof(Organization))!.GetTableName();
        var query = $@"
                INSERT INTO ""{organizationTable}""
                (""Id"", ""Tin"", ""Name"", ""TermsAccepted"", ""TermsAcceptanceDate"")
                VALUES (@Id, @Tin, @Name, @TermsAccepted, @TermsAcceptanceDate);";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Tin", tin),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("TermsAccepted", true),
            new NpgsqlParameter("TermsAcceptanceDate", DateTime.UtcNow));
    }
}
