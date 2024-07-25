using System;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class ModifiedContractsToUseRecipientIdAndRemovedCertificatesTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ModifiedContractsToUseRecipientIdAndRemovedCertificatesTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task ApplyMigration_ContractsShouldBeDeleted()
    {
        await using (var dbContext = new ApplicationDbContext(options))
        {
            var migrator = dbContext.GetService<IMigrator>();

            await migrator.MigrateAsync("20240701111541_RemovedSyncPosition");

            for (int i = 0; i < 5; i++)
            {
                await InsertOldContract(dbContext, i);
            }
            await dbContext.SaveChangesAsync();

            var applyMigration = () => migrator.MigrateAsync("20240723091045_ModifiedContractsToUseRecipientIdAndRemovedCertificates");
            await applyMigration.Should().NotThrowAsync();
        }

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Contracts.Count().Should().Be(0);
        }
    }

    private static async Task InsertOldContract(ApplicationDbContext dbContext, int contractNumber)
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
            new NpgsqlParameter("Id", Guid.NewGuid()),
            new NpgsqlParameter("GSRN", "123123123" + contractNumber),
            new NpgsqlParameter("GridArea", "DK1"),
            new NpgsqlParameter("MeteringPointType", 1),
            new NpgsqlParameter("MeteringPointOwner", "DummyOwner"),
            new NpgsqlParameter("StartDate", DateTimeOffset.UtcNow),
            new NpgsqlParameter("Created", DateTimeOffset.UtcNow),
            new NpgsqlParameter("WalletUrl", "DummyUrl"),
            new NpgsqlParameter("WalletPublicKey", new byte[]{}),
            new NpgsqlParameter("ContractNumber", contractNumber),
            new NpgsqlParameter("Technology_FuelCode", ""),
            new NpgsqlParameter("Technology_TechCode", "")
        };

        await dbContext.Database.ExecuteSqlRawAsync(contractQuery, contractFields);
    }
}
