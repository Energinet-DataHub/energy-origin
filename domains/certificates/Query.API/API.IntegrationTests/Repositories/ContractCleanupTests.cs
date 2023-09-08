//using API.ContractService;
//using API.DataSyncSyncer;
//using API.IntegrationTests.Helpers;
//using API.IntegrationTests.Testcontainers;
//using CertificateValueObjects;
//using FluentAssertions;
//using Marten;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;

//namespace API.IntegrationTests.Repositories;

//public class ContractCleanupTests : IClassFixture<MartenDbContainer>, IAsyncLifetime
//{
//    private readonly IDocumentStore store;
//    private const string BadMeteringPointInDemoEnvironment = "571313000000000200";

//    public ContractCleanupTests(MartenDbContainer dbContainer)
//        => store = DocumentStore.For(opts => opts.Connection(dbContainer.ConnectionString));

//    [Fact]
//    public async Task deletes_everything_for_571313000000000200_when_multiple_owners()
//    {
//        var contract1 = new CertificateIssuingContract
//        {
//            ContractNumber = 0,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        var contract2 = new CertificateIssuingContract
//        {
//            ContractNumber = 1,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner2",
//            MeteringPointType = MeteringPointType.Production
//        };

//        await InsertContracts(contract1, contract2);

//        await store.CleanupContracts(CancellationToken.None);

//        var numberOfContracts = await GetTotalNumberOfContracts();
//        numberOfContracts.Should().Be(0);
//    }

//    [Fact]
//    public async Task deletes_only_everything_for_571313000000000200_when_multiple_owners()
//    {
//        var contract1 = new CertificateIssuingContract
//        {
//            ContractNumber = 0,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        var contract2 = new CertificateIssuingContract
//        {
//            ContractNumber = 1,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner2",
//            MeteringPointType = MeteringPointType.Production
//        };

//        var contract3 = new CertificateIssuingContract
//        {
//            ContractNumber = 0,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = GsrnHelper.GenerateRandom(),
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        await InsertContracts(contract1, contract2, contract3);

//        await store.CleanupContracts(CancellationToken.None);

//        var numberOfContracts = await GetTotalNumberOfContracts();
//        numberOfContracts.Should().Be(1);
//    }

//    [Fact]
//    public async Task deletes_nothing_for_571313000000000200_when_same_owner()
//    {
//        var contract1 = new CertificateIssuingContract
//        {
//            ContractNumber = 0,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        var contract2 = new CertificateIssuingContract
//        {
//            ContractNumber = 1,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = BadMeteringPointInDemoEnvironment,
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        await InsertContracts(contract1, contract2);

//        await store.CleanupContracts(CancellationToken.None);

//        var numberOfContracts = await GetTotalNumberOfContracts();
//        numberOfContracts.Should().Be(2);
//    }

//    [Fact]
//    public async Task deletes_nothing_for_other_gsrns_when_multiple_owners_()
//    {
//        var gsrn = GsrnHelper.GenerateRandom();

//        var contract1 = new CertificateIssuingContract
//        {
//            ContractNumber = 0,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = gsrn,
//            MeteringPointOwner = "owner1",
//            MeteringPointType = MeteringPointType.Production
//        };

//        var contract2 = new CertificateIssuingContract
//        {
//            ContractNumber = 1,
//            Created = DateTimeOffset.Now,
//            StartDate = DateTimeOffset.Now,
//            EndDate = null,
//            GridArea = "DK1",
//            GSRN = gsrn,
//            MeteringPointOwner = "owner2",
//            MeteringPointType = MeteringPointType.Production
//        };

//        await InsertContracts(contract1, contract2);

//        await store.CleanupContracts(CancellationToken.None);

//        var numberOfContracts = await GetTotalNumberOfContracts();
//        numberOfContracts.Should().Be(2);
//    }

//    private async Task InsertContracts(params CertificateIssuingContract[] certificateIssuingContract)
//    {
//        await using var session = store.OpenSession();

//        session.Insert(certificateIssuingContract);

//        await session.SaveChangesAsync();
//    }

//    private async Task<int> GetTotalNumberOfContracts()
//        => (await store.QuerySession().Query<CertificateIssuingContract>().ToListAsync()).Count;

//    private async Task DeleteAllContracts()
//    {
//        await using var session = store.OpenSession();
//        var allContracts = await session.Query<CertificateIssuingContract>().ToListAsync();

//        foreach (var contract in allContracts)
//        {
//            session.Delete(contract);
//        }

//        await session.SaveChangesAsync();
//    }

//    public Task InitializeAsync()
//    {
//        return Task.CompletedTask;
//    }

//    async Task IAsyncLifetime.DisposeAsync()
//    {
//        await DeleteAllContracts();
//        store.Dispose();
//    }
//}
