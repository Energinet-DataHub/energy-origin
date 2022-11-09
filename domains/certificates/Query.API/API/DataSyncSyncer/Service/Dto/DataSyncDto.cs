using CertificateEvents;

namespace API.DataSyncSyncer.Service.Datasync;

public record DataSyncDto(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    EnergyMeasurementQuality Quality
);
