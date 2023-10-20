using System.Threading;
using System.Threading.Tasks;

namespace API.Claiming.Automation;

public interface IClaimService
{
    Task Run(CancellationToken stoppingToken);
}
