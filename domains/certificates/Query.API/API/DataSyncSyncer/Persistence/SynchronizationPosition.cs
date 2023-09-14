namespace API.DataSyncSyncer.Persistence;

public class SynchronizationPosition
{
    public string GSRN { get; set; } = "";
    public long SyncedTo { get; set; }
}
