using System;
using DataContext.ValueObjects;

namespace API.MeasurementsSyncer;

public record MeteringPointSyncInfo(Gsrn Gsrn,
    DateTimeOffset StartSyncDate,
    DateTimeOffset? EndSyncDate,
    string MeteringPointOwner,
    MeteringPointType MeteringPointType,
    string GridArea,
    Guid RecipientId,
    Technology? Technology);
