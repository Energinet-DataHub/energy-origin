using System;
using System.Numerics;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Persistence;
using API.IntegrationTests.Testcontainers;
using CertificateEvents;
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

    private readonly CertificateIssuingContract contract = new()
    {
        GSRN = "1234",
        GridArea = "SomeGridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "SomeMeteringPointOwner",
        StartDate = DateTimeOffset.Now.AddDays(-1)
    };

    public SyncStateTests(MartenDbContainer martenDbContainer)
    {
        this.martenDbContainer = martenDbContainer;
    }

    [Fact]
    public async Task GetPeriodStartTime_OneCertificateInStore_ReturnsNewestDate()
    {
        var createdEvent = new ProductionCertificateCreated(Guid.NewGuid(),
            "SomeGridArea",
            new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds()),
            new Technology("SomeFuelCode", "SomeTechCode"),
            "SomeMeteringPointOwner",
            new ShieldedValue<string>("1234", BigInteger.Zero),
            new ShieldedValue<long>(42, BigInteger.Zero));

        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(martenDbContainer.ConnectionString);
        });
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), createdEvent);
        await session.SaveChangesAsync();

        var syncState = new SyncState(store, Mock.Of<ILogger<SyncState>>());

        var actualPeriodStartTime = await syncState.GetPeriodStartTime2(contract);

        actualPeriodStartTime.Should().Be(createdEvent.Period.DateTo);
    }
}
