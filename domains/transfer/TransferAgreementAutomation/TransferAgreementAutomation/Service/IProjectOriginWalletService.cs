using System.Threading.Tasks;
using Transfer.Domain.Entities;

namespace TransferAgreementAutomation.Worker.Service;

public interface IProjectOriginWalletService
{

    Task TransferCertificates(TransferAgreement transferAgreement);
}
