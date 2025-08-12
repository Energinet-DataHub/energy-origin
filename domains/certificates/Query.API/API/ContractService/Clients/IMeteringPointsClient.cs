using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Models;

namespace API.ContractService.Clients;

public interface IMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);
}
