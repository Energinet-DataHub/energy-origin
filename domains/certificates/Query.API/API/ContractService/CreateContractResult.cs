using System.Collections.Generic;
using DataContext.Models;

namespace API.ContractService;

public abstract record CreateContractResult
{
    public record Success(List<CertificateIssuingContract> CertificateIssuingContracts) : CreateContractResult;

    public record GsrnNotFound(string Gsrn) : CreateContractResult;
    public record CannotBeUsedForIssuingCertificates(string Gsrn) : CreateContractResult;
    public record ContractAlreadyExists(CertificateIssuingContract? Existing) : CreateContractResult;

    private CreateContractResult() { }
}
