using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.ContractService.Repositories;

public interface ICertificateIssuingContractRepository
{
    Task Save(CertificateIssuingContract certificateIssuingContract);
    Task<CertificateIssuingContract?> GetByGsrn(string gsrn);
    Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetAllContracts();
}
