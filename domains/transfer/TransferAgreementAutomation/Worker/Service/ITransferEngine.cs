using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace TransferAgreementAutomation.Worker.Service;

public interface ITransferEngine
{
    bool IsSupported(TransferAgreement transferAgreement);

    Task TransferCertificates(TransferAgreement transferAgreement, CancellationToken cancellationToken = default);
}
