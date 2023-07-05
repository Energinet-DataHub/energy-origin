using System;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Testcontainers;
using CertificateValueObjects;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.IntegrationTests.Repositories;

public class SyncStateTests :
    IClassFixture<MartenDbContainer>
{
    private readonly MartenDbContainer martenDbContainer;

    public SyncStateTests(MartenDbContainer martenDbContainer)
    {
        this.martenDbContainer = martenDbContainer;
    }

    [Fact]
    public async Task GetPeriodStartTime_NoDataInStore_ReturnsContractStartDate()
    {
        CertificateIssuingContract contract = new()
        {
            GSRN = "1234",
            GridArea = "SomeGridArea",
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = "SomeMeteringPointOwner",
            StartDate = DateTimeOffset.Now.AddDays(-1)
        };

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(contract.StartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        CertificateIssuingContract contract = new()
        {
            GSRN = "1235",
            GridArea = "SomeGridArea",
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = "SomeMeteringPointOwner",
            StartDate = DateTimeOffset.Now.AddDays(-1)
        };
        var position = new SyncPosition(Guid.NewGuid(), contract.GSRN, DateTimeOffset.Now.ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(position.SyncedTo);
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStoreButIsBeforeContractStartDate_ReturnsContractStartDate()
    {
        CertificateIssuingContract contract = new()
        {
            GSRN = "1236",
            GridArea = "SomeGridArea",
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = "SomeMeteringPointOwner",
            StartDate = DateTimeOffset.Now.AddDays(-1)
        };
        var position = new SyncPosition(Guid.NewGuid(), contract.GSRN, contract.StartDate.AddHours(-1).ToUnixTimeSeconds());

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), position);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(contract.StartDate.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetPeriodStartTime_DatabaseCommunicationFailure_ReturnsNull()
    {
        CertificateIssuingContract contract = new()
        {
            GSRN = "1237",
            GridArea = "SomeGridArea",
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = "SomeMeteringPointOwner",
            StartDate = DateTimeOffset.Now.AddDays(-1)
        };
        var storeMock = new Mock<IDocumentStore>();
        storeMock.Setup(m => m.QuerySession())
            .Throws<Exception>()
            .Verifiable();

        var syncState = new SyncState(storeMock.Object, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime(contract);

        actualPeriodStartTime.Should().Be(null);
        storeMock.Verify();
    }
}
