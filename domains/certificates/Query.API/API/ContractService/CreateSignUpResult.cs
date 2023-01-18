namespace API.ContractService;

public abstract record CreateSignUpResult
{
    public record Success(CertificateIssuingContract CertificateIssuingContract) : CreateSignUpResult;

    public record GsrnNotFound : CreateSignUpResult;

    public record NotProductionMeteringPoint : CreateSignUpResult;

    public record SignUpAlreadyExists(CertificateIssuingContract Existing) : CreateSignUpResult;

    private CreateSignUpResult() { }
}
