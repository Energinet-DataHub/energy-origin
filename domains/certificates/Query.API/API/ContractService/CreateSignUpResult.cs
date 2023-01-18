namespace API.ContractService;

public abstract record CreateSignUpResult
{
    public record Success(CertificateGenerationSignUp CertificateGenerationSignUp) : CreateSignUpResult;

    public record GsrnNotFound : CreateSignUpResult;

    public record NotProductionMeteringPoint : CreateSignUpResult;

    public record SignUpAlreadyExists(CertificateGenerationSignUp Existing) : CreateSignUpResult;

    private CreateSignUpResult() { }
}
