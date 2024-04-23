using DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Testing.Testcontainers;
using Xunit;
using DataContext.Models;
using DataContext.ValueObjects;
using Testing.Helpers;
using FluentAssertions;

namespace API.IntegrationTests.Shared.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class ModifyWalletUrlsOnContractsMigrationTests
{
    private readonly PostgresContainer postgresContainer;

    public ModifyWalletUrlsOnContractsMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        postgresContainer = integrationTestFixture.PostgresContainer;
    }

    [Fact]
    public async Task ApplyMigration_ContractsShouldHaveNewWalletUrl()
    {
        await using (var dbContext = GetDbContext())
        {
            var migrator = dbContext.GetService<IMigrator>();

            await migrator.MigrateAsync("20240408104920_AddSlidingWindow");

            for (int i = 0; i < 5; i++)
            {
                await InsertContract(dbContext, i);
            }
            await dbContext.SaveChangesAsync();

            var applyMigration = () => migrator.MigrateAsync("20240423100351_ModifyWalletUrlsOnContracts");
            await applyMigration.Should().NotThrowAsync();
        }

        await using (var dbContext = GetDbContext())
        {
            dbContext.Contracts.Count().Should().Be(5);
            dbContext.Contracts.ToList().ForEach(contract =>
            {
                contract.WalletUrl.Should().Be("http://foo/v1/slices/" + contract.ContractNumber);
            });
        }
    }

    private async Task InsertContract(ApplicationDbContext dbContext, int contractNumber)
    {
        await dbContext.Contracts.AddAsync(new CertificateIssuingContract
        {
            WalletUrl = "http://foo/wallet-api/" + contractNumber,
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

    private ApplicationDbContext GetDbContext()
    {
        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(postgresContainer.ConnectionString).Options;
        return new ApplicationDbContext(contextOptions);
    }
}
