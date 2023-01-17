using System.Collections.Generic;
using System.Linq;
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

    public Task<MeteringPointSignup?> GetByGsrn(long gsrn)
    {
        return session.Query<MeteringPointSignup>().Where(x => x.GSRN == gsrn).SingleOrDefaultAsync();
    }

    public Task<IEnumerable<MeteringPointSignup>> GetAll() => throw new System.NotImplementedException();

    public Task<IEnumerable<MeteringPointSignup>> GetByMeteringPointOwner(string meteringPointOwner) => throw new System.NotImplementedException();
}
