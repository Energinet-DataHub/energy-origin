using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificatesEvents;

[EventModelVersion("EnergyMeasured", 1)]
public record EnergyMeasured(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    EnergyMeasurementQuality Quality
) : EventModel;

public enum EnergyMeasurementQuality { Measured, Revised, Calculated, Estimated }
