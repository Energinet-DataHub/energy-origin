using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace API.Claim.Automation;

public class ClaimWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new System.NotImplementedException();
    }
}
