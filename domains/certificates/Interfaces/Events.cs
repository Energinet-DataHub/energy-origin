namespace Interfaces;


// measurement
public enum Quality { Measured, Revised, Calculated, Estimated, }

[EventModelVersion("EnergyMeasured", 1)]
public record EnergyMeasured(
    string GSRN, // Metering Point ID
    long DateFrom,
    long DateTo,
    long Quantity, // Wh
    Quality Quality
) : EventModel;

// cert created
public record CertificateCreated(
) : EventModel;

// cert finalized


// cert rejected
