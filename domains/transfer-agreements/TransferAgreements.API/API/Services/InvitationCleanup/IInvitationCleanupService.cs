using System.Threading;
using System.Threading.Tasks;

namespace API.Services.InvitationCleanup;

public interface IInvitationCleanupService
{
    Task Run(CancellationToken stoppingToken);
}
