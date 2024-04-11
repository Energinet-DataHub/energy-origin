using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using System;
using System.Threading.Tasks;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

public class UpdateNullTechnologyCodesMigrationTests : IAsyncDisposable
{
    private readonly PostgresContainer container = new();

    [Fact]
    public async Task ApplyMigration_WhenContractsHaveEmptyTechnologyCodes_UpdatesFieldsCorrectly()
    {
        await using var dbContext = await CreateDbContext();

        var migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync("20231107095405_AddTechnologyToCertificateIssuingContract");

        var productionContractId = Guid.NewGuid();
        var consumptionContractId = Guid.NewGuid();
        await InsertContractWithEmptyTechnologyFields(dbContext, productionContractId, "123456789", "DK1", MeteringPointType.Production);
        await InsertContractWithEmptyTechnologyFields(dbContext, consumptionContractId, "369236923", "DK1", MeteringPointType.Consumption);

        var productionContractBeforeMigration = await dbContext.Contracts.FindAsync(productionContractId);
        productionContractBeforeMigration!.Technology!.FuelCode.Should().Be("");
        productionContractBeforeMigration!.Technology.TechCode.Should().Be("");

        var consumptionContractBeforeMigration = await dbContext.Contracts.FindAsync(consumptionContractId);
        consumptionContractBeforeMigration!.Technology!.FuelCode.Should().Be("");
        consumptionContractBeforeMigration!.Technology!.TechCode.Should().Be("");

        var applyMigration = () => migrator.MigrateAsync("20231115155411_UpdateNullTechnologyCodes");
        await applyMigration.Should().NotThrowAsync();

        await using var newDbContext = await CreateDbContext();

        var productionContractAfterMigration = await newDbContext.Contracts.FindAsync(productionContractId);
        productionContractAfterMigration!.Technology!.FuelCode.Should().Be("F00000000");
        productionContractAfterMigration.Technology.TechCode.Should().Be("T070000");

        var consumptionContractAfterMigration = await newDbContext.Contracts.FindAsync(consumptionContractId);
        consumptionContractAfterMigration!.Technology.Should().BeNull();
    }

    private static async Task InsertContractWithEmptyTechnologyFields(TransferDbContext dbContext, Guid id, string gsrn, string gridArea, MeteringPointType meteringPointType)
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

        object[] contractFields =
        {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("GSRN", gsrn),
            new NpgsqlParameter("GridArea", gridArea),
            new NpgsqlParameter("MeteringPointType", (int)meteringPointType),
            new NpgsqlParameter("MeteringPointOwner", "DummyOwner"),
            new NpgsqlParameter("StartDate", DateTimeOffset.UtcNow),
            new NpgsqlParameter("Created", DateTimeOffset.UtcNow),
            new NpgsqlParameter("WalletUrl", "DummyUrl"),
            new NpgsqlParameter("WalletPublicKey", new byte[] { }),
            new NpgsqlParameter("ContractNumber", 1),
            new NpgsqlParameter("Technology_FuelCode", ""),
            new NpgsqlParameter("Technology_TechCode", "")
        };

        await dbContext.Database.ExecuteSqlRawAsync(contractQuery, contractFields);
    }

    private async Task<TransferDbContext> CreateDbContext()
    {
        await container.InitializeAsync();
        var contextOptions = new DbContextOptionsBuilder<TransferDbContext>()
                                .UseNpgsql(container.ConnectionString)
                                .Options;

        var dbContext = new TransferDbContext(contextOptions);
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
