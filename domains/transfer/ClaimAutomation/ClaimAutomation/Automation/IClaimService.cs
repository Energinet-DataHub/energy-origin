using System.Threading;
using System.Threading.Tasks;

namespace ClaimAutomation.Worker.Automation;

public interface IClaimService
{
    Task Run(CancellationToken stoppingToken);
}
