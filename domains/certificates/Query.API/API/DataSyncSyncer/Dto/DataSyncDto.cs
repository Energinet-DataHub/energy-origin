using IntegrationEvents;

namespace API.DataSyncSyncer.Dto;

public record DataSyncDto(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);
