using API.ContractService;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Testcontainers;
using CertificateValueObjects;
using FluentAssertions;
using Marten;
using System;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class ContractCleanupTests : IClassFixture<MartenDbContainer>
{
    private readonly MartenDbContainer dbContainer;
    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";

    public ContractCleanupTests(MartenDbContainer dbContainer)
    {
        this.dbContainer = dbContainer;
    }

    [Fact]
    public async Task deletes_everything_for_multiple_owners_for_571313000000000200()
    {
        using var store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(store, contract1, contract2);

        await store.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetNumberOfContracts(store);
        numberOfContracts.Should().Be(0);

        await DeleteAllContracts(store);
    }

    [Fact]
    public async Task deletes_only_everything_for_multiple_owners_for_571313000000000200()
    {
        using var store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        var contract3 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = GsrnHelper.GenerateRandom(),
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(store, contract1, contract2, contract3);

        await store.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetNumberOfContracts(store);
        numberOfContracts.Should().Be(1);

        await DeleteAllContracts(store);
    }

    [Fact]
    public async Task deletes_nothing_for_same_owner_for_571313000000000200()
    {
        using var store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = BadMeteringPointInDemoEnvironment,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(store, contract1, contract2);

        await store.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetNumberOfContracts(store);
        numberOfContracts.Should().Be(2);

        await DeleteAllContracts(store);
    }

    [Fact]
    public async Task deletes_nothing_for_multiple_owners_for_other_gsrns()
    {
        using var store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

        var gsrn = GsrnHelper.GenerateRandom();

        var contract1 = new CertificateIssuingContract
        {
            ContractNumber = 0,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = gsrn,
            MeteringPointOwner = "owner1",
            MeteringPointType = MeteringPointType.Production
        };

        var contract2 = new CertificateIssuingContract
        {
            ContractNumber = 1,
            Created = DateTimeOffset.Now,
            StartDate = DateTimeOffset.Now,
            EndDate = null,
            GridArea = "DK1",
            GSRN = gsrn,
            MeteringPointOwner = "owner2",
            MeteringPointType = MeteringPointType.Production
        };

        await InsertContracts(store, contract1, contract2);

        await store.CleanupContracts(CancellationToken.None);

        var numberOfContracts = await GetNumberOfContracts(store);
        numberOfContracts.Should().Be(2);

        await DeleteAllContracts(store);
    }

    private static async Task InsertContracts(IDocumentStore store, params CertificateIssuingContract[] certificateIssuingContract)
    {
        await using var session = store.OpenSession();

        session.Insert(certificateIssuingContract);

        await session.SaveChangesAsync();
    }

    private static async Task<int> GetNumberOfContracts(IDocumentStore store)
    {
        await using var session = store.QuerySession();
        var allContracts = await session.Query<CertificateIssuingContract>().ToListAsync();

        return allContracts.Count;
    }

    private static async Task DeleteAllContracts(IDocumentStore store)
    {
        await using var session = store.OpenSession();
        var allContracts = await session.Query<CertificateIssuingContract>().ToListAsync();

        foreach (var contract in allContracts)
        {
            session.Delete(contract);
        }

        await session.SaveChangesAsync();
    }
}
