using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace API.CertificateGenerationSignupService.Repositories;

internal class CertificateGenerationSignUpRepository : ICertificateGenerationSignUpRepository
{
    private readonly IDocumentSession session;

    public CertificateGenerationSignUpRepository(IDocumentSession session)
    {
        this.session = session;
    }

    public Task Save(CertificateGenerationSignUp certificateGenerationSignUp)
    {
        session.Insert(certificateGenerationSignUp);
        return session.SaveChangesAsync();
    }

    public Task<CertificateGenerationSignUp?> GetByGsrn(string gsrn) => session
        .Query<CertificateGenerationSignUp>()
        .Where(x => x.GSRN == gsrn)
        .SingleOrDefaultAsync();

    public Task<IReadOnlyList<CertificateGenerationSignUp>> GetAllMeteringPointOwnerSignUps(string meteringPointOwner, CancellationToken cancellationToken) => session
        .Query<CertificateGenerationSignUp>()
        .Where(x => x.MeteringPointOwner == meteringPointOwner)
        .ToListAsync(cancellationToken);

    public Task<CertificateGenerationSignUp?> GetById(Guid id, CancellationToken cancellationToken) => session
        .LoadAsync<CertificateGenerationSignUp>(id, cancellationToken);
}
