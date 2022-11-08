using CertificateEvents.Primitives;

namespace CertificateEvents;

public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    Period Period,
    long Quantity,
    EnergyMeasurementQuality Quality
);
