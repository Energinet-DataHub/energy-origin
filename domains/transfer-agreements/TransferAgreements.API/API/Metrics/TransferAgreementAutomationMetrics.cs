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

    private Counter<int> TransferRetriesPerCertificate { get; }

    private const string certificateIdKey = "CertificateId";
    public TransferAgreementAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfTransferAgreementsOnLastRun = meter.CreateObservableGauge<int>("transfer-agreements-on-last-run", () => numberOfTransferAgreementsOnLastRun);
        CertificatesTransferredOnLastRun = meter.CreateObservableGauge<int>("certificates-transferred-on-last-run", () => certificatesTransferredOnLastRun);
        TransferRetriesPerCertificate = meter.CreateCounter<int>("transfer-retries-per-certificate");
    }

    public void SetNumberOfTransferAgreementsOnLastRun(int transferAgreementsOnLastRun) =>
        numberOfTransferAgreementsOnLastRun = transferAgreementsOnLastRun;

    public void SetCertificatesTransferredOnLastRun(int certificatesTransferred) =>
        certificatesTransferredOnLastRun = certificatesTransferred;

    public void TransferRetry(Guid certificateId) =>
        TransferRetriesPerCertificate.Add(1,
            GetKeyValuePair(certificateIdKey, certificateId));

    private static KeyValuePair<string, object?> GetKeyValuePair(string key, object? value) => new(key, value);
}
