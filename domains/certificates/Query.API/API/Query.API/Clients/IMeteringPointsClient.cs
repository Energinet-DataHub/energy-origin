using System.Threading;
using System.Threading.Tasks;

namespace API.Query.API.Clients;

public interface IMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);
}
