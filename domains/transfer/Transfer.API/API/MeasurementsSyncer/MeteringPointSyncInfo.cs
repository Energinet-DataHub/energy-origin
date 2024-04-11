using System;

namespace API.MeasurementsSyncer;

public record MeteringPointSyncInfo(string GSRN, DateTimeOffset StartSyncDate, string MeteringPointOwner);
