using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace API.ContractService.Repositories;

internal class CertificateIssuingContractRepository : ICertificateIssuingContractRepository
{
    private readonly IDocumentSession session;

    public CertificateIssuingContractRepository(IDocumentSession session)
    {
        this.session = session;
    }

    public Task Save(CertificateIssuingContract certificateIssuingContract)
    {
        session.Insert(certificateIssuingContract);
        return session.SaveChangesAsync();
    }

    public Task<CertificateIssuingContract?> GetByGsrn(string gsrn, CancellationToken cancellationToken) => session
        .Query<CertificateIssuingContract>()
        .Where(x => x.GSRN == gsrn)
        .SingleOrDefaultAsync(cancellationToken);

    public Task<IReadOnlyList<CertificateIssuingContract>> GetAllMeteringPointOwnerContracts(string meteringPointOwner, CancellationToken cancellationToken) => session
        .Query<CertificateIssuingContract>()
        .Where(x => x.MeteringPointOwner == meteringPointOwner)
        .ToListAsync(cancellationToken);

    public Task<CertificateIssuingContract?> GetById(Guid id, CancellationToken cancellationToken) => session
        .LoadAsync<CertificateIssuingContract>(id, cancellationToken);
}
