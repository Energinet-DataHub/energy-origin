using System.Threading;
using System.Threading.Tasks;

namespace API.KeyIssuer.Repositories;

public interface IKeyIssuingRepository
{
    Task Save(KeyIssuingDocument keyIssuingDocument);

    Task<KeyIssuingDocument?> GetByMeteringPointOwner(string meteringPointOwner,
        CancellationToken cancellationToken);
}
