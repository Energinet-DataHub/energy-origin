using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace API.Metrics;

public class TransferAgreementAutomationMetrics : ITransferAgreementAutomationMetrics
{
    public const string MetricName = "TransferAgreementAutomation";

    private int numberOfTransferAgreementsOnLastRun = 0;
    private ObservableGauge<int> NumberOfTransferAgreementsOnLastRun { get; }
    private Counter<int> TransferPerCertificate { get; }

    private const string certificateIdKey = "CertificateId";
    private const string registryIdKey = "Registry";
    public TransferAgreementAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfTransferAgreementsOnLastRun = meter.CreateObservableGauge<int>("transfer-agreements-on-last-run", () => numberOfTransferAgreementsOnLastRun);
        TransferPerCertificate = meter.CreateCounter<int>("transfer-per-certificate");
    }

    public void SetNumberOfTransferAgreements(int transferAgreementsOnLastRun) =>
        numberOfTransferAgreementsOnLastRun = transferAgreementsOnLastRun;

    public void AddTransferAttempt(string registry, Guid certificateId) =>
        TransferPerCertificate.Add(1,
            CreateTag(registryIdKey, registry),
            CreateTag(certificateIdKey, certificateId));

    private static KeyValuePair<string, object?> CreateTag(string key, object? value) => new(key, value);
}
