using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Models;

namespace API.ContractService.Clients.Internal;

public interface IAdminPortalMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);
}
