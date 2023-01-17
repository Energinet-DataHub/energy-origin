using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupService;

public interface ICertificateGenerationSignupService
{
    Task<CreateSignupResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeteringPointSignup>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken);

    Task<MeteringPointSignup?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken);
}
