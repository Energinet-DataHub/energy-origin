using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    EnergyMeasurementQuality Quality
);
