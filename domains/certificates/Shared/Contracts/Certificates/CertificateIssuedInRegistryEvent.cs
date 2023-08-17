using System;

namespace Contracts.Certificates;

public record CertificateIssuedInRegistryEvent(
    Guid CertificateId,
    string RegistryName,
    byte[] BlindingValue,
    long Quantity,
    byte[] WalletPublicKey,
    string WalletUrl,
    uint WalletPosition
);
