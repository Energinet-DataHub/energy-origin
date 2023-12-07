using System.Threading;
using System.Threading.Tasks;

namespace TransferAgreementAutomation.Worker.Service;

public interface ITransferAgreementsAutomationService
{
    Task Run(CancellationToken stoppingToken);
}
