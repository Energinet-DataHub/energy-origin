using System.Numerics;

namespace CertificateEvents.Primitives;

public record ShieldedValue<T>(
    T Value, // stamdata
    BigInteger R  // group.RandomR()
);
