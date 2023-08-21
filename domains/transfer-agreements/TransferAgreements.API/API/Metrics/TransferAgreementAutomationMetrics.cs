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
    private const string registryIdKey = "Registry";
    public TransferAgreementAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfTransferAgreementsOnLastRun = meter.CreateObservableGauge<int>("transfer-agreements-on-last-run", () => numberOfTransferAgreementsOnLastRun);
        CertificatesTransferredOnLastRun = meter.CreateObservableGauge<int>("certificates-transferred-on-last-run", () => certificatesTransferredOnLastRun);
        TransferRetriesPerCertificate = meter.CreateCounter<int>("transfer-retries-per-certificate");
    }

    public void SetNumberOfTransferAgreementsOnLastRun(int transferAgreementsOnLastRun) =>
        numberOfTransferAgreementsOnLastRun = transferAgreementsOnLastRun;

    public void AddCertificatesTransferred(int certificatesTransferred) =>
        certificatesTransferredOnLastRun += certificatesTransferred;

    public void ResetCertificatesTransferred() =>
        certificatesTransferredOnLastRun = 0;

    public void AddTransferAttempt(string registry, Guid certificateId) =>
        TransferRetriesPerCertificate.Add(1,
            CreateTag(registryIdKey, registry),
            CreateTag(certificateIdKey, certificateId));

    private static KeyValuePair<string, object?> CreateTag(string key, object? value) => new(key, value);
}
