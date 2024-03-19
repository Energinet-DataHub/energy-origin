namespace TransferAgreementAutomation.Worker.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreements(int transferAgreementsOnLastRun);
        void SetNumberOfCertificates(int certificatesOnLastRun);

        void ResetCertificatesTransferred();
    }
}
