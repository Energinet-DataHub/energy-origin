using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupService;

public interface ICertificateGenerationSignupService
{
    Task<CreateSignupResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken);
}
