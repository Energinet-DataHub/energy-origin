using System;
using DataContext.ValueObjects;

namespace Contracts.Certificates.CertificateIssuedInRegistry.V1;

public record CertificateIssuedInRegistryEvent(
    Guid CertificateId,
    string RegistryName,
    byte[] BlindingValue,
    long Quantity,
    MeteringPointType MeteringPointType,
    byte[] WalletPublicKey,
    string WalletUrl,
    uint WalletDepositEndpointPosition
);
