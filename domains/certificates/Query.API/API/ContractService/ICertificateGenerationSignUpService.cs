using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.ContractService;

public interface ICertificateGenerationSignUpService
{
    Task<CreateSignUpResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);

    Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
}
