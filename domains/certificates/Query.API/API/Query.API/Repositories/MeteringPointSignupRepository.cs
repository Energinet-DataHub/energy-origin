using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace API.Query.API.Repositories;

public class MeteringPointSignupRepository : IMeteringPointSignupRepository
{
    private readonly IDocumentSession session;

    public MeteringPointSignupRepository(IDocumentSession session)
    {
        this.session = session;
    }

    public Task Save(MeteringPointSignup meteringPointSignup)
    {
        session.Insert(meteringPointSignup);
        return session.SaveChangesAsync();
    }

    public Task<MeteringPointSignup?> GetByGsrn(string gsrn) => session
        .Query<MeteringPointSignup>()
        .Where(x => x.GSRN == gsrn)
        .SingleOrDefaultAsync();

    public Task<IReadOnlyList<MeteringPointSignup>> GetAllMeteringPointOwnerSignUps(string owner) => session
        .Query<MeteringPointSignup>()
        .Where(x => x.MeteringPointOwner == owner)
        .ToListAsync();

    public Task<MeteringPointSignup?> GetByDocumentId(string documentId, CancellationToken cancellationToken) => session
        .LoadAsync<MeteringPointSignup>(documentId, cancellationToken);
}
