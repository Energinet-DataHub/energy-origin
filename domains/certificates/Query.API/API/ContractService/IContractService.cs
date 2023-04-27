using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.ContractService;

public interface IContractService
{
    Task<CreateContractResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, DateTimeOffset? endDate, CancellationToken cancellationToken);

    Task<EndContractResult> EndContract(string meteringPointOwner, Guid contractId, DateTimeOffset endDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateIssuingContract>> GetByGSRN(string gsrn, CancellationToken cancellationToken);
}
