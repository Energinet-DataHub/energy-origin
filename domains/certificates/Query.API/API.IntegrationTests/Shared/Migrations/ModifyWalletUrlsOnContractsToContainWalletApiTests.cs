using System;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class ModifyWalletUrlsOnContractsToContainWalletApiTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public ModifyWalletUrlsOnContractsToContainWalletApiTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task ApplyMigration_ContractsShouldHaveNewWalletUrl()
    {
        await using (var dbContext = new ApplicationDbContext(options))
        {
            var migrator = dbContext.GetService<IMigrator>();

            await migrator.MigrateAsync("20240423100351_ModifyWalletUrlsOnContracts");

            for (int i = 0; i < 5; i++)
            {
                await InsertContract(dbContext, i);
            }
            await dbContext.SaveChangesAsync();

            var applyMigration = () => migrator.MigrateAsync("20240503112223_ModifyWalletUrlsOnContractsToContainWalletApi");
            await applyMigration.Should().NotThrowAsync();
        }

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Contracts.Count().Should().Be(5);
            dbContext.Contracts.ToList().ForEach(contract =>
            {
                contract.WalletUrl.Should().Be("http://foo/wallet-api/v1/slices");
            });
        }
    }

    private async Task InsertContract(ApplicationDbContext dbContext, int contractNumber)
    {
        await dbContext.Contracts.AddAsync(new CertificateIssuingContract
        {
            WalletUrl = "http://foo/v1/slices",
            ContractNumber = contractNumber,
            Created = DateTimeOffset.Now.ToUniversalTime(),
            EndDate = null,
            GSRN = GsrnHelper.GenerateRandom(),
            GridArea = "DK1",
            Id = Guid.NewGuid(),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.Now.ToUniversalTime(),
            Technology = new Technology("", ""),
        });
    }
}