using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupService;

public interface ICertificateGenerationSignUpService
{
    Task<CreateSignupResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateGenerationSignUp>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);

    Task<CertificateGenerationSignUp?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
}
