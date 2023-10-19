using System.Threading;
using System.Threading.Tasks;

namespace API.Transfer.TransferAgreementsAutomation.Service;

public interface ITransferAgreementsAutomationService
{
    Task Run(CancellationToken stoppingToken);
}
