using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupService.Repositories;

public interface IMeteringPointSignupRepository
{
    Task Save(MeteringPointSignup meteringPointSignup);
    Task<MeteringPointSignup?> GetByGsrn(string gsrn);
    Task<IReadOnlyList<MeteringPointSignup>> GetAllMeteringPointOwnerSignUps(string meteringPointOwner);
    Task<MeteringPointSignup?> GetByDocumentId(Guid documentId, CancellationToken cancellationToken);
}
