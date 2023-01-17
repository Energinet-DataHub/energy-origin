using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;

namespace API.CertificateGenerationSignupService.Repositories;

internal class MeteringPointSignupRepository : IMeteringPointSignupRepository
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

    public Task<IReadOnlyList<MeteringPointSignup>> GetAllSignUps(string owner) => session
        .Query<MeteringPointSignup>()
        .Where(x => x.MeteringPointOwner == owner)
        .ToListAsync();

    public Task<IEnumerable<MeteringPointSignup>> GetAll() => throw new System.NotImplementedException();

    public Task<IEnumerable<MeteringPointSignup>> GetByMeteringPointOwner(string meteringPointOwner) => throw new System.NotImplementedException();
}
