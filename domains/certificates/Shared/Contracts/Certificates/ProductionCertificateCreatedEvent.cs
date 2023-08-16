using System;
using CertificateValueObjects;

namespace Contracts.Certificates;

public record ProductionCertificateCreatedEvent(Guid CertificateId,
    string GridArea,
    Period Period,
    Technology Technology,
    string MeteringPointOwner,
    Gsrn Gsrn,
    byte[] BlindingValue,
    long Quantity, //TODO: long?
    byte[] WalletPublicKey,
    string WalletUrl,
    int WalletPosition
);
