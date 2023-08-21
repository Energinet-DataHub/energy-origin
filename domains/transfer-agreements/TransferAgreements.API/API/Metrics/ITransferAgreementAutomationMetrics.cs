using System;

namespace API.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreementsOnLastRun(int numberOfTransferAgreementsOnLastRun);
        void AddCertificatesTransferred(int certificatesTransferred);
        void ResetCertificatesTransferred();
        void AddTransferAttempt(string registry, Guid certificateId);
    }
}
