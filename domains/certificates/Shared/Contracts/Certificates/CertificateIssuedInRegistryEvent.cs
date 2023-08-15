using System;

namespace Contracts.Certificates;

public record CertificateIssuedInRegistryEvent(Guid CertificateId,
    byte[] BlindingValue,
    long Quantity, //TODO: long?
    byte[] WalletPublicKey,
    string WalletUrl);
