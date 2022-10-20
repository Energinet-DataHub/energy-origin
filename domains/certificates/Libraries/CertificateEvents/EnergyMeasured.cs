using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("EnergyMeasured", 1)]
public record EnergyMeasured(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    EnergyMeasurementQuality Quality
) : EventModel;

public enum EnergyMeasurementQuality { Measured, Revised, Calculated, Estimated }
