namespace API.Transfer.TransferAgreementsAutomation.Metrics
{
    public interface ITransferAgreementAutomationMetrics
    {
        void SetNumberOfTransferAgreements(int transferAgreementsOnLastRun);
        void SetNumberOfCertificates(int certificatesOnLastRun);

        void AddTransferError();
        void ResetTransferErrors();
        void ResetCertificatesTransferred();
    }
}
