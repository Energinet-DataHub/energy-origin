using System.Threading;
using System.Threading.Tasks;

namespace API.TransferAgreementsAutomation;

public interface ITransferAgreementsAutomationService
{
    Task Run(CancellationToken stoppingToken);
}
