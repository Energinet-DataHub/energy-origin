using System.Numerics;

namespace Domain;

/// <summary>
/// This class is made to support Pedersen-commitment.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Value">Stamdata</param>
/// <param name="R">Random number</param>
public record ShieldedValue<T>(
    T Value, // stamdata
    BigInteger R  // group.RandomR()
);
