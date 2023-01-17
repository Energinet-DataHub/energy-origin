using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupServiceBla.Repositories;

public interface ICertificateGenerationSignUpRepository
{
    Task Save(CertificateGenerationSignUp certificateGenerationSignUp);
    Task<CertificateGenerationSignUp?> GetByGsrn(string gsrn);
    Task<IReadOnlyList<CertificateGenerationSignUp>> GetAllMeteringPointOwnerSignUps(string meteringPointOwner, CancellationToken cancellationToken);
    Task<CertificateGenerationSignUp?> GetById(Guid id, CancellationToken cancellationToken);
}
