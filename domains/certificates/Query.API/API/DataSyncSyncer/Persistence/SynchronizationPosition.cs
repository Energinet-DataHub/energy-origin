using Marten.Schema;

namespace API.DataSyncSyncer.Persistence;

public class SynchronizationPosition
{
    [Identity]
    public string GSRN { get; set; } = "";
    public long SyncedTo { get; set; }
}
