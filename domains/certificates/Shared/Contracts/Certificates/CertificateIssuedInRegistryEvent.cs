using System;

namespace Contracts.Certificates;

public record CertificateIssuedInRegistryEvent(
    Guid CertificateId,
    string RegistryName,
    byte[] BlindingValue,
    long Quantity, //TODO: long?
    byte[] WalletPublicKey,
    string WalletUrl,
    uint WalletPosition
);
