using System;

namespace API.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreementsOnLastRun(int numberOfTransferAgreementsOnLastRun);
        void SetCertificatesTransferredOnLastRun(int certificatesTransferredOnLastRun);
        void SetErrorsOnLastRun(int errorsOnLastRun);
        void TransferError(Guid certificateId);
    }
}
