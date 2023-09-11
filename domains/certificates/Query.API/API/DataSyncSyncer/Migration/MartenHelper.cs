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

        await using var session = store.QuerySession();

        return await session.Query<CertificateIssuingContract>().ToListAsync();
    }
}
