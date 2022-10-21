using System;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("CertificateCreated", 1)]
public record CertificateCreated(
    Guid CertificateId, // Guid.newGuid()
    string GridArea, // stamdata
    Period Period,
    Technology Technology, // stamdata
    byte[] OwnerPublicKey, // stamdata - meterpoint owners key
    ShieldedValue<string> ShieldedGSRN, //Group.commit(EnergyMeasured.GSRN)
    ShieldedValue<long> ShieldedQuantity //Group.commit(EnergyMeasured.Quantity)
) : EventModel;
