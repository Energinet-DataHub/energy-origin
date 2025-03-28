using System;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

namespace Contracts.Certificates;

public record ProductionCertificateCreatedEvent(
    Guid CertificateId,
    string GridArea,
    Period Period,
    Technology Technology,
    string MeteringPointOwner,
    Gsrn Gsrn,
    byte[] BlindingValue,
    long Quantity,
    byte[] WalletPublicKey,
    string WalletUrl,
    uint WalletDepositEndpointPosition
);
