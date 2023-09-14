using System.Threading;
using System.Threading.Tasks;

namespace API.Services.ConnectionInvitationCleanup;

public interface IConnectionInvitationCleanupService
{
    Task Run(CancellationToken stoppingToken);
}
