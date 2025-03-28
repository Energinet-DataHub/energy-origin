using System;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;

namespace Contracts.Certificates;

public record ConsumptionCertificateCreatedEvent(
    Guid CertificateId,
    string GridArea,
    Period Period,
    string MeteringPointOwner,
    Gsrn Gsrn,
    byte[] BlindingValue,
    long Quantity,
    byte[] WalletPublicKey,
    string WalletUrl,
    uint WalletDepositEndpointPosition
);
