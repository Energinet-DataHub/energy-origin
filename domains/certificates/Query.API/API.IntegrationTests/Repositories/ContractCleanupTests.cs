using API.ContractService;
using API.Data;
using API.DataSyncSyncer;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using CertificateValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.Core;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class ContractCleanupTests : IClassFixture<PostgresContainer>, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;
    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";
    private readonly ConcurrentBag<ApplicationDbContext?> disposableContexts = new();

    public ContractCleanupTests(PostgresContainer dbContainer)
    {
        factory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();

        ApplicationDbContext CreateDbContext(CallInfo _)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            disposableContexts.Add(dbContext);
            return dbContext;
        }

        factory.CreateDbContextAsync().Returns(CreateDbContext);
        factory.CreateDbContext().Returns(CreateDbContext);
    }

    [Fact]
    public async Task deletes_everything_for_571313000000000200_when_multiple_owners()
    {
        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(contract1, contract2);

        await factory.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetTotalNumberOfContracts();
        numberOfContracts.Should().Be(0);
    }

    [Fact]
    public async Task deletes_only_everything_for_571313000000000200_when_multiple_owners()
    {
        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        var contract3 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = GsrnHelper.GenerateRandom(),
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(contract1, contract2, contract3);

        await factory.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetTotalNumberOfContracts();
        numberOfContracts.Should().Be(1);
    }

    [Fact]
    public async Task deletes_nothing_for_571313000000000200_when_same_owner()
    {
        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(contract1, contract2);

        await factory.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetTotalNumberOfContracts();
        numberOfContracts.Should().Be(2);
    }

    [Fact]
    public async Task deletes_nothing_for_other_gsrns_when_multiple_owners_()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = gsrn,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = null,
            GridArea = "DK1",
            GSRN = gsrn,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(contract1, contract2);

        await factory.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetTotalNumberOfContracts();
        numberOfContracts.Should().Be(2);
    }

    private async Task InsertContracts(params CertificateIssuingContract[] certificateIssuingContract)
    {
        await using var context = await factory.CreateDbContextAsync();

        context.Contracts.AddRange(certificateIssuingContract);

        await context.SaveChangesAsync();
    }

    private async Task<int> GetTotalNumberOfContracts()
        => (await (await factory.CreateDbContextAsync()).Contracts.ToListAsync()).Count;
    
    public void Dispose()
    {
        using var dbContext = factory.CreateDbContext();
        var tableName = dbContext.Model.FindEntityType(typeof(CertificateIssuingContract))?.GetTableName() ?? "";
        dbContext.Database.ExecuteSqlRaw($"TRUNCATE TABLE \"{tableName}\"");

        foreach (var disposableContext in disposableContexts)
        {
            disposableContext?.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
