using System;

namespace API.DataSyncSyncer.Persistence;


//Id is required by MartenDb to query: https://martendb.io/documents/identity.html
public record SyncPosition(Guid Id, string Gsrn, long SyncedTo);
