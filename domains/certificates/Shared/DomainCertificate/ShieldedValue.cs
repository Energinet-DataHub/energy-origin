using System.Numerics;

namespace DomainCertificate;

/// <summary>
/// This class is made to support Pedersen-commitment.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Shielded">Stamdata</param>
/// <param name="R">Random number</param>
public record ShieldedValue<T>(
    T Shielded, // stamdata
    BigInteger R  // group.RandomR()
);
