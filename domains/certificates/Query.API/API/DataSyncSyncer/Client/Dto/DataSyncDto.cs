using IntegrationEvents;

namespace API.DataSyncSyncer.Client.Dto;

public record DataSyncDto(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);
