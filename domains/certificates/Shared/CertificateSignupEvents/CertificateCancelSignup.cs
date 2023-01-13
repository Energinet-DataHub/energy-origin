namespace CertificateSignupEvents;

public record CertificateCancelSignup(
    Guid MeteringPointOwner,
    string GSRN
    );
