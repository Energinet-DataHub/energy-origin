using CertificateEvents;
using Marten.Events.Projections;
using Marten.Schema;

namespace API.DataSyncSyncer.Persistence;

public class SyncStateProjection : MultiStreamAggregation<SyncStateView, string>
{
    public SyncStateProjection()
        => Identity<ProductionCertificateCreated>(e => e.ShieldedGSRN.Value);

    public void Apply(ProductionCertificateCreated @event, SyncStateView view)
    {
        if (@event.Period.DateTo > view.SyncDateTo)
            view.SyncDateTo = @event.Period.DateTo;
    }
}

public class SyncStateView
{
    [Identity]
    public string GSRN { get; set; } = "";

    public long SyncDateTo { get; set; }
}
