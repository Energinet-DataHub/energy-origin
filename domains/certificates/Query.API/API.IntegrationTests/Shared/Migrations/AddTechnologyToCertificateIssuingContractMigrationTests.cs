using DataContext;
using DataContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

public class AddTechnologyToCertificateIssuingContractMigrationTests : IAsyncDisposable
{
    private readonly PostgresContainer container = new();

    [Fact]
    public async Task ApplyMigration_WhenExistingDataInDatabase_AddsTechnologyColumnsSuccessfully()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20231017070514_AddedConsumptionCertificates");

        await InsertOldContract(dbContext, Guid.NewGuid(), "123456789", "DK1");
        await InsertOldContract(dbContext, Guid.NewGuid(), "987654321", "DK2");

        var applyMigration = () => migrator.MigrateAsync();
        await applyMigration.Should().NotThrowAsync();

        var contractsInDb = dbContext.Contracts.ToList();

        contractsInDb.Count.Should().Be(2);
    }

    private static async Task InsertOldContract(ApplicationDbContext dbContext, Guid id, string gsrn, string gridArea)
    {
        var contractsTable = dbContext.Model.FindEntityType(typeof(CertificateIssuingContract))!.GetTableName();

        var contractQuery =
            $@"INSERT INTO ""{contractsTable}"" (
            ""Id"",
            ""GSRN"",
            ""GridArea"",
            ""MeteringPointType"",
            ""MeteringPointOwner"",
            ""StartDate"",
            ""Created"",
            ""WalletUrl"",
            ""WalletPublicKey"",
            ""ContractNumber"")
        VALUES (
            @Id,
            @GSRN,
            @GridArea,
            @MeteringPointType,
            @MeteringPointOwner,
            @StartDate,
            @Created,
            @WalletUrl,
            @WalletPublicKey,
            @ContractNumber)";

        object[] contractFields = {
        new NpgsqlParameter("Id", id),
        new NpgsqlParameter("GSRN", gsrn),
        new NpgsqlParameter("GridArea", gridArea),
        new NpgsqlParameter("MeteringPointType", 1),
        new NpgsqlParameter("MeteringPointOwner", "DummyOwner"),
        new NpgsqlParameter("StartDate", DateTimeOffset.UtcNow),
        new NpgsqlParameter("Created", DateTimeOffset.UtcNow),
        new NpgsqlParameter("WalletUrl", "DummyUrl"),
        new NpgsqlParameter("WalletPublicKey", new byte[]{}),
        new NpgsqlParameter("ContractNumber", 1)
    };

        await dbContext.Database.ExecuteSqlRawAsync(contractQuery, contractFields);
    }

    private async Task<ApplicationDbContext> CreateNewCleanDatabase()
    {
        await container.InitializeAsync();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
