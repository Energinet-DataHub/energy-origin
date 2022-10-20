using System;
using System.Numerics;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificatesEvents;

[EventModelVersion("CertificateCreated", 1)]
public record CertificateCreated(
    Guid CertificateId, // Guid.newGuid()
    string GridArea, // stamdata
    Period Period, // EnergyMeasured.DateFrom
    Technology Technology, // stamdata
    byte[] OwnerPublicKey, // stamdata - meterpoint owners key
    ShieldedValue<string> ShieldedGSRN, //Group.commit(EnergyMeasured.GSRN)
    ShieldedValue<long> ShieldedQuantity //Group.commit(EnergyMeasured.Quantity)
) : EventModel;

public record Period(
    long DateFrom, // EnergyMeasured.DateFrom
    long DateTo  // EnergyMeasured.DateTo
);

public record Technology(
    string FuelCode, // stamdata
    string TechCode  // stamdata
);

public record ShieldedValue<T>(
    T Value, // stamdata
    BigInteger R  // group.RandomR()
);
