using System;

namespace API.DataSyncSyncer;

public record MeteringPointSyncInfo(string GSRN, DateTimeOffset StartSyncDate, string MeteringPointOwner);
