using System.Threading.Tasks;
using System.Threading;

namespace API.Claim.Automation;

public interface IClaimService
{
    Task Run(CancellationToken stoppingToken);
}
