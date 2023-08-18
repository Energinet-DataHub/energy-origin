using System;

namespace API.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreementsOnLastRun(int numberOfTransferAgreementsOnLastRun);
        void SetCertificatesTransferredOnLastRun(int certificatesTransferredOnLastRun);
        void AddTransferAttempt(Guid certificateId);
    }
}
