namespace CertificateEvents.Primitives;

public record Period(
    long DateFrom, // EnergyMeasured.DateFrom
    long DateTo  // EnergyMeasured.DateTo
);
