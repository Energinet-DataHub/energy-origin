using System;
using CertificateEvents.Primitives;

namespace CertificateSignupEvents;

public record CertificateSignup(
    Guid MeteringPointOwner,
    ShieldedValue<string> ShieldedGSRN, // Really unsure wether or not this should be shielded
    bool signedUp
    )
