using API.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using System;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

public class AddTransferAgreementProposalsAndDeleteConnectionsMigrationTests : MigrationsTestBase
{
    [Fact]
    public async Task ApplyMigration_WhenExistingDataInDatabase_Success()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20231102084120_AddClaimAutomationArgument");

        await InsertOldConnection(dbContext, Guid.NewGuid(), Guid.NewGuid(), "12345678", Guid.NewGuid(), "11223344");
        await InsertOldConnectionInvitation(dbContext, Guid.NewGuid(), Guid.NewGuid(), "12345678", DateTimeOffset.UtcNow);

        var applyMigration = () => migrator.Migrate("20231123093303_AddTransferAgreementProposalsAndDeleteConnections");
        applyMigration.Should().NotThrow();
    }

    private static async Task InsertOldConnection(ApplicationDbContext dbContext, Guid id, Guid companyAId, string companyATin, Guid companyBId, string companyBTin)
    {
        var connectionsTable = "Connections";

        var connectionsQuery =
            $"INSERT INTO \"{connectionsTable}\" (\"Id\", \"CompanyAId\", \"CompanyATin\", \"CompanyBId\", \"CompanyBTin\") VALUES (@Id, @CompanyAId, @CompanyATin, @CompanyBId, @CompanyBTin)";
        var connectionsFields = new[]
        {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("CompanyAId", companyAId),
            new NpgsqlParameter("CompanyATin", companyATin),
            new NpgsqlParameter("CompanyBId", companyBId),
            new NpgsqlParameter("CompanyBTin", companyBTin)
        };

        await dbContext.Database.ExecuteSqlRawAsync(connectionsQuery, connectionsFields);
    }

    private static async Task InsertOldConnectionInvitation(ApplicationDbContext dbContext, Guid id, Guid senderCompanyId, string senderCompanyTin, DateTimeOffset createdAt)
    {
        var table = "ConnectionInvitations";

        var query =
            $"INSERT INTO \"{table}\" (\"Id\", \"SenderCompanyId\", \"SenderCompanyTin\", \"CreatedAt\") VALUES (@Id, @SenderCompanyId, @SenderCompanyTin, @CreatedAt)";
        var fields = new[]
        {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("SenderCompanyId", senderCompanyId),
            new NpgsqlParameter("SenderCompanyTin", senderCompanyTin),
            new NpgsqlParameter("CreatedAt", createdAt)
        };

        await dbContext.Database.ExecuteSqlRawAsync(query, fields);
    }
}
