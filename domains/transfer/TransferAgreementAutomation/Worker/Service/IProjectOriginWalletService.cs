using System.Threading.Tasks;
using TransferAgreementAutomation.Worker.Models;

namespace TransferAgreementAutomation.Worker.Service;

public interface IProjectOriginWalletService
{

    Task TransferCertificates(TransferAgreementDto transferAgreement);
}
