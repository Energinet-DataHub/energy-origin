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

    public Task<KeyIssuingDocument?> GetByMeteringPointOwner(string meteringPointOwner,
        CancellationToken cancellationToken) =>
        session.DocumentStore.QuerySession().LoadAsync<KeyIssuingDocument>(meteringPointOwner, cancellationToken);
}
