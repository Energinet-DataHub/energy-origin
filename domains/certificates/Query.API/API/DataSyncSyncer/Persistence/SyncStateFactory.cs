using System;
using System.Collections.Generic;

namespace API.DataSyncSyncer.Persistence;

public static class SyncStateFactory
{
    public static ISyncState? CreateSyncState(Dictionary<string, DateTimeOffset> state) => new SyncState(state);
}
