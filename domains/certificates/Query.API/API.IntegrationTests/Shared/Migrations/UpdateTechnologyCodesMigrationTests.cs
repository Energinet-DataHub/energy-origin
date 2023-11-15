using System;
using System.Threading.Tasks;
using API.ContractService;
using API.Data;
using API.IntegrationTests.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

public class UpdateTechnologyCodesMigrationTests : IAsyncDisposable
{
    private readonly PostgresContainer container = new();

    [Fact]
    public async Task ApplyMigration_WhenContractsHaveEmptyTechnologyCodes_UpdatesFieldsCorrectly()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync("20231107095405_AddTechnologyToCertificateIssuingContract");

        var contractId = Guid.NewGuid();
        await InsertContractWithEmptyTechnologyFields(dbContext, contractId, "123456789", "DK1");

        var contractBeforeMigration = await dbContext.Contracts.FindAsync(contractId);
        contractBeforeMigration!.Technology.FuelCode.Should().Be("");
        contractBeforeMigration.Technology.TechCode.Should().Be("");

        var applyMigration = () => migrator.MigrateAsync("20231114121811_UpdateEmptyTechnologyCodes");
        await applyMigration.Should().NotThrowAsync();

        await using var newDbContext = await CreateNewCleanDatabase();
        var updatedContract = await newDbContext.Contracts.FindAsync(contractId);
        updatedContract!.Technology.FuelCode.Should().Be("F00000000");
        updatedContract.Technology.TechCode.Should().Be("T070000");
    }

    private static async Task InsertContractWithEmptyTechnologyFields(ApplicationDbContext dbContext, Guid id, string gsrn, string gridArea)
    {
        var contractsTable = dbContext.Model.FindEntityType(typeof(CertificateIssuingContract))!.GetTableName();

        var contractQuery = $@"INSERT INTO ""{contractsTable}"" (
                        ""Id"",
                        ""GSRN"",
                        ""GridArea"",
                        ""MeteringPointType"",
                        ""MeteringPointOwner"",
                        ""StartDate"",
                        ""Created"",
                        ""WalletUrl"",
                        ""WalletPublicKey"",
                        ""ContractNumber"",
                        ""Technology_FuelCode"",
                        ""Technology_TechCode"")
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
                        @ContractNumber,
                        @Technology_FuelCode,
                        @Technology_TechCode
                    )";

        object[] contractFields = {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("GSRN", gsrn),
            new NpgsqlParameter("GridArea", gridArea),
            new NpgsqlParameter("MeteringPointType", (object?)0),
            new NpgsqlParameter("MeteringPointOwner", "DummyOwner"),
            new NpgsqlParameter("StartDate", DateTimeOffset.UtcNow),
            new NpgsqlParameter("Created", DateTimeOffset.UtcNow),
            new NpgsqlParameter("WalletUrl", "DummyUrl"),
            new NpgsqlParameter("WalletPublicKey", new byte[]{}),
            new NpgsqlParameter("ContractNumber", 1),
            new NpgsqlParameter("Technology_FuelCode", ""),
            new NpgsqlParameter("Technology_TechCode", "")
        };

        await dbContext.Database.ExecuteSqlRawAsync(contractQuery, contractFields);
    }

    private async Task<ApplicationDbContext> CreateNewCleanDatabase()
    {
        await container.InitializeAsync();
        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                                .UseNpgsql(container.ConnectionString)
                                .Options;

        var dbContext = new ApplicationDbContext(contextOptions);
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
