using System;
using Marten.Schema;

namespace API.DataSyncSyncer.Persistence;


//Id is required by MartenDb to query: https://martendb.io/documents/identity.html
public record SyncPosition(Guid Id, string GSRN, long SyncedTo);

public class SynchronizationPosition
{
    [Identity]
    public string GSRN { get; set; }
    public long SyncedTo { get; set; }
}
