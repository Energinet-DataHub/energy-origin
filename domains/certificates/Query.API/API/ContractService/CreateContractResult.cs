namespace API.ContractService;

public abstract record CreateContractResult
{
    public record Success(CertificateIssuingContract CertificateIssuingContract) : CreateContractResult;

    public record GsrnNotFound : CreateContractResult;

    public record NotProductionMeteringPoint : CreateContractResult;

    public record ContractAlreadyExists(CertificateIssuingContract Existing) : CreateContractResult;

    private CreateContractResult() { }
}
