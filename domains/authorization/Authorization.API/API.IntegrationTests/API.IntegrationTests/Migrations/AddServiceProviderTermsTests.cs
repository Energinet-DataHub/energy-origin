using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class AddServiceProviderTermsTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AddServiceProviderTermsTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(newDatabaseInfo.ConnectionString)
            .Options;
    }

    [Fact]
    public async Task ApplyingMigration_AddsServiceProviderTermsTableWithCorrectColumns()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241022085628_AddUniqueConsentIndex");

        await migrator.MigrateAsync("20241118010705_AddServiceProviderTerms");

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var commandText = @"
                SELECT column_name, data_type, is_nullable
                FROM information_schema.columns
                WHERE table_name = 'ServiceProviderTerms';";

        var columns = new List<(string ColumnName, string DataType, string IsNullable)>();

        await using (var command = new NpgsqlCommand(commandText, connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var isNullable = reader.GetString(2);

                columns.Add((columnName, dataType, isNullable));
            }
        }

        columns.Should().ContainSingle(c => c.ColumnName == "Id" && c.DataType == "uuid" && c.IsNullable == "NO");
        columns.Should().ContainSingle(c => c.ColumnName == "Version" && c.DataType == "integer" && c.IsNullable == "NO");
        columns.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplyingMigration_AddsServiceProviderTermsFieldsToExistingOrganization()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20241022085628_AddUniqueConsentIndex");

        var orgId = Guid.NewGuid();
        await InsertTestOrganization(dbContext, orgId, "12345678", "Test Organization");

        await migrator.MigrateAsync("20241118010705_AddServiceProviderTerms");

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var commandText = @"
                SELECT ""Id"", ""ServiceProviderTermsAccepted"", ""ServiceProviderTermsVersion"", ""ServiceProviderTermsAcceptanceDate""
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

            int? serviceProviderTermsVersion = null;
            if (!reader.IsDBNull(reader.GetOrdinal("ServiceProviderTermsVersion")))
            {
                serviceProviderTermsVersion = reader.GetInt32(reader.GetOrdinal("ServiceProviderTermsVersion"));
            }

            DateTimeOffset? serviceProviderTermsAcceptanceDate = null;
            if (!reader.IsDBNull(reader.GetOrdinal("ServiceProviderTermsAcceptanceDate")))
            {
                serviceProviderTermsAcceptanceDate = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("ServiceProviderTermsAcceptanceDate"));
            }

            serviceProviderTermsAccepted.Should().BeFalse();
            serviceProviderTermsVersion.Should().BeNull();
            serviceProviderTermsAcceptanceDate.Should().BeNull();
        }

        organizationFound.Should().BeTrue("Organization should exist in the database.");
    }

    private static async Task InsertTestOrganization(ApplicationDbContext dbContext, Guid id, string tin, string name)
    {
        var organizationTable = dbContext.Model.FindEntityType(typeof(Organization))!.GetTableName();
        var query = $@"
                INSERT INTO ""{organizationTable}""
                (""Id"", ""Tin"", ""Name"", ""TermsAccepted"", ""TermsVersion"", ""TermsAcceptanceDate"")
                VALUES (@Id, @Tin, @Name, @TermsAccepted, @TermsVersion, @TermsAcceptanceDate);";

        await dbContext.Database.ExecuteSqlRawAsync(query,
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Tin", tin),
            new NpgsqlParameter("Name", name),
            new NpgsqlParameter("TermsAccepted", true),
            new NpgsqlParameter("TermsVersion", 1),
            new NpgsqlParameter("TermsAcceptanceDate", DateTime.UtcNow));
    }
}
