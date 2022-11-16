using CertificateEvents;
using IntegrationEvents;

namespace API.DataSyncSyncer.Service.Datasync;

public record DataSyncDto(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);
