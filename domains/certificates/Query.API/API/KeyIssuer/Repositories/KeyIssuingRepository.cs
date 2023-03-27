using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace API.KeyIssuer.Repositories;

internal class KeyIssuingRepository : IKeyIssuingRepository
{
    private readonly IDocumentSession session;

    public KeyIssuingRepository(IDocumentSession session)
    {
        this.session = session;
    }

    public Task Save(KeyIssuingDocument keyIssuingDocument)
    {
        session.Insert(keyIssuingDocument);
        return session.SaveChangesAsync();
    }

    public Task<KeyIssuingDocument?> GetByMeteringPointOwner(string meteringPointOwner, CancellationToken cancellationToken) => session
            .Query<KeyIssuingDocument>()
            .Where(x => x.MeteringPointOwner == meteringPointOwner)
            .SingleOrDefaultAsync(cancellationToken);
}
