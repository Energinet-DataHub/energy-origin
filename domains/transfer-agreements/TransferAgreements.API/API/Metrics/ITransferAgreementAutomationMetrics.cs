using System;

namespace API.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreements(int transferAgreementsOnLastRun);
        void AddTransferAttempt(string registry, Guid certificateId);
        void SetNumberOfCertificates(int certificatesOnLastRun);
    }
}
