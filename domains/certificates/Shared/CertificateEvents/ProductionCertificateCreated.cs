using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

public record ProductionCertificateCreated(
    Guid CertificateId, // Guid.newGuid()
    string GridArea, // stamdata
    Period Period,
    Technology Technology, // stamdata
    string MeteringPointOwner, // stamdata - meterpoint owners key
    ShieldedValue<string> ShieldedGSRN, //Group.commit(EnergyMeasured.GSRN)
    ShieldedValue<long> ShieldedQuantity //Group.commit(EnergyMeasured.Quantity)
);
