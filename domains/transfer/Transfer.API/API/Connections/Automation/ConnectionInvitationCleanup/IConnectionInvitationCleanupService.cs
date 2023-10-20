using System.Threading;
using System.Threading.Tasks;

namespace API.Connections.Automation.ConnectionInvitationCleanup;

public interface IConnectionInvitationCleanupService
{
    Task Run(CancellationToken stoppingToken);
}
