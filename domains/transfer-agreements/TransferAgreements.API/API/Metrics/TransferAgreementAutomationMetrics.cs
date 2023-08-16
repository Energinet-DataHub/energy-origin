using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace API.Metrics;

public class TransferAgreementAutomationMetrics : ITransferAgreementAutomationMetrics
{
    public const string MetricName = "TransferAgreementAutomation";

    private int numberOfTransferAgreementsOnLastRun = 0;
    private ObservableGauge<int> NumberOfTransferAgreementsOnLastRun { get; }
    private int certificatesTransferredOnLastRun = 0;
    private ObservableGauge<int> CertificatesTransferredOnLastRun { get; }
    private int errorsOnLastRun = 0;
    private ObservableGauge<int> ErrorsOnLastRun { get; }

    private Counter<int> TransferErrors { get; }

    private const string certificateIdKey = "CertificateId";
    public TransferAgreementAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfTransferAgreementsOnLastRun = meter.CreateObservableGauge<int>("transfer-agreements-on-last-run", () => numberOfTransferAgreementsOnLastRun);
        CertificatesTransferredOnLastRun = meter.CreateObservableGauge<int>("certificates-transferred-on-last-run", () => certificatesTransferredOnLastRun);
        ErrorsOnLastRun = meter.CreateObservableGauge<int>("errors-on-last-run", () => errorsOnLastRun);
        TransferErrors = meter.CreateCounter<int>("transfer-error");
    }

    public void SetNumberOfTransferAgreementsOnLastRun(int transferAgreementsOnLastRun) =>
        numberOfTransferAgreementsOnLastRun = transferAgreementsOnLastRun;

    public void SetCertificatesTransferredOnLastRun(int certificatesTransferred) =>
        certificatesTransferredOnLastRun = certificatesTransferred;

    public void SetErrorsOnLastRun(int numberOfErrors) =>
        errorsOnLastRun = numberOfErrors;

    public void TransferError(Guid certificateId) =>
        TransferErrors.Add(1,
            GetKeyValuePair(certificateIdKey, certificateId));

    private static KeyValuePair<string, object?> GetKeyValuePair(string key, object? value) => new(key, value);
}
