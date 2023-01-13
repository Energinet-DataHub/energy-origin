namespace CertificateSignupEvents;

public record CertificateSignup(
    Guid MeteringPointOwner,
    string GSRN
    );
