using System.Threading;
using System.Threading.Tasks;

namespace API.ContractService.Clients;

public interface IMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);

    Task<Meteringpoint.V1.MeteringPointsResponse>
        GetMeteringPoints(Meteringpoint.V1.OwnedMeteringPointsRequest request);
}
