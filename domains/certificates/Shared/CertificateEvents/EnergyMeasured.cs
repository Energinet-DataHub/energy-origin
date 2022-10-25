using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("EnergyMeasured", 1)]
public record EnergyMeasured(
    string GSRN,
    Period Period,
    long Quantity,
    EnergyMeasurementQuality Quality
) : MeasurementBaseEvent;

public enum EnergyMeasurementQuality { Measured, Revised, Calculated, Estimated }
