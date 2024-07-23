using System;
using DataContext.ValueObjects;

namespace API.MeasurementsSyncer;

public record MeteringPointSyncInfo(string GSRN,
    DateTimeOffset StartSyncDate,
    string MeteringPointOwner,
    MeteringPointType MeteringPointType,
    string GridArea,
    Guid RecipientId,
    Technology? Technology);
