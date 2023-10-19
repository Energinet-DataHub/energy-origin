using System.Threading;
using System.Threading.Tasks;

namespace API.Connections.Api.Services.ConnectionInvitationCleanup;

public interface IConnectionInvitationCleanupService
{
    Task Run(CancellationToken stoppingToken);
}
