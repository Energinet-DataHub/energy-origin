using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer.Persistence;
using CertificateEvents;
using Marten;
using Marten.Schema;
using Microsoft.Extensions.Configuration;

namespace API.DataSyncSyncer.Migration;

public class MartenHelper
{
    private readonly IConfiguration configuration;

    public MartenHelper(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<IReadOnlyList<SynchronizationPosition>> GetSynchronizationPositions()
    {
        var martenConnectionString = this.configuration.GetConnectionString("Marten")!;
        var store = DocumentStore.For(o =>
        {
            o.Connection(martenConnectionString);

            o.Schema.For<SynchronizationPosition>().Identity(x => x.GSRN);
        });

        //await using var session = store.LightweightSession();

        //session.Store(
        //    new SynchronizationPosition { GSRN = "1234567", SyncedTo = 42 },
        //    new SynchronizationPosition { GSRN = "7654321", SyncedTo = 400 });

        //await session.SaveChangesAsync();

        await using var session = store.QuerySession();

        return await session.Query<SynchronizationPosition>().ToListAsync();
    }

    public async Task<IReadOnlyList<ProductionCertificateCreated>> GetEvents()
    {
        var martenConnectionString = this.configuration.GetConnectionString("Marten")!;
        var store = DocumentStore.For(o =>
        {
            o.Connection(martenConnectionString);
        });

        //await using var session = store.LightweightSession();

        //var stream = Guid.NewGuid();
        //session.Events.Append(stream,
        //    new ProductionCertificateCreated(stream, "DK1", new Period(1, 42), new Technology("fuel", "tech"),
        //        "owner42", new ShieldedValue<string>("gsrn", BigInteger.Zero), new ShieldedValue<long>(52, BigInteger.Zero), new byte[] { 1, 2, 3 }));

        //session.Events.Append(stream, new ProductionCertificateIssued(stream));

        //await session.SaveChangesAsync();

        await using var session = store.QuerySession();

        var queryRawEventDataOnly = await session.Events.QueryRawEventDataOnly<ProductionCertificateCreated>().ToListAsync();

        var filtered = queryRawEventDataOnly.Where(c => c.BlindingValue != null && c.BlindingValue.Length > 0);

        return filtered.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<CertificateIssuingContract>> GetContracts()
    {
        var martenConnectionString = this.configuration.GetConnectionString("Marten")!;
        var store = DocumentStore.For(o =>
        {
            o.Connection(martenConnectionString);

            o.Schema
                .For<CertificateIssuingContract>()
                .UniqueIndex(UniqueIndexType.Computed, "uidx_gsrn_contractnumber", c => c.GSRN, c => c.ContractNumber);
        });

        //await using var session = store.LightweightSession();

        //session.Store(new CertificateIssuingContract
        //{
        //    ContractNumber = 0,
        //    MeteringPointType = MeteringPointType.Production,
        //    WalletPublicKey = new byte[] { 1, 2, 3 },
        //    GridArea = "DK2",
        //    WalletUrl = "foo",
        //    GSRN = "7654321",
        //    MeteringPointOwner = "owner42",
        //    StartDate = DateTimeOffset.UtcNow,
        //    EndDate = null,
        //    Created = DateTimeOffset.UtcNow

        //});

        //await session.SaveChangesAsync();

        await using var session = store.QuerySession();

        return await session.Query<CertificateIssuingContract>().ToListAsync();
    }
}
