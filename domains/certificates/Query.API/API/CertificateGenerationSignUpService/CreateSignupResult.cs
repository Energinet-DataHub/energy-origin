namespace API.CertificateGenerationSignUpService;

public abstract record CreateSignupResult
{
    public record Success(CertificateGenerationSignUp CertificateGenerationSignUp) : CreateSignupResult;

    public record GsrnNotFound : CreateSignupResult;

    public record NotProductionMeteringPoint : CreateSignupResult;

    public record SignupAlreadyExists(CertificateGenerationSignUp Existing) : CreateSignupResult;

    private CreateSignupResult() { }
}
