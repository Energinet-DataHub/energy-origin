using System.Threading.Tasks;
using TransferAgreement = TransferAgreementAutomation.Worker.Models.TransferAgreement;

namespace TransferAgreementAutomation.Worker.Service;

public interface IProjectOriginWalletService
{

    Task TransferCertificates(TransferAgreement transferAgreement);
}
