using System.Threading.Tasks;
using DataContext.Models;

namespace TransferAgreementAutomation.Worker.Service;

public interface IProjectOriginWalletService
{

    Task TransferCertificates(TransferAgreement transferAgreement);
}
