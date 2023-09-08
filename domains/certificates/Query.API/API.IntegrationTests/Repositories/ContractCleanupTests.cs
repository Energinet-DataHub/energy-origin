using API.ContractService;
using API.Data;
using API.DataSyncSyncer;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using CertificateValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class ContractCleanupTests : IClassFixture<PostgresContainer>, IAsyncLifetime
{
    private readonly IDbContextFactory<ApplicationDbContext> factory;
    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";
    private readonly ConcurrentBag<ApplicationDbContext?> disposableContexts = new();

    public ContractCleanupTests(PostgresContainer dbContainer)
    {
        factory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();

        factory.CreateDbContextAsync().Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            disposableContexts.Add(dbContext);
            return dbContext;
        });
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

        context.AddRange(certificateIssuingContract); //TODO: ...

        await context.SaveChangesAsync();
    }

    private async Task<int> GetTotalNumberOfContracts()
        => (await (await factory.CreateDbContextAsync()).Contracts.ToListAsync()).Count;

    private async Task DeleteAllContracts()
    {
        await using var context = await factory.CreateDbContextAsync();
        var allContracts = await context.Contracts.ToListAsync();

        foreach (var contract in allContracts)
        {
            context.Remove(contract);
        }

        await context.SaveChangesAsync();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DeleteAllContracts();
    }
}
