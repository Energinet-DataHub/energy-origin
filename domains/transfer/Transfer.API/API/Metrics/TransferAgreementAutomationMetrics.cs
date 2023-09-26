using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace API.Metrics;

public class TransferAgreementAutomationMetrics : ITransferAgreementAutomationMetrics
{
    public const string MetricName = "TransferAgreementAutomation";

    private int numberOfTransferAgreementsOnLastRun = 0;
    private int numberOfCertificatesOnLastRun = 0;
    private int numberOfCertificatesWithTransferErrors = 0;
    private ObservableGauge<int> NumberOfTransferAgreementsOnLastRun { get; }
    private ObservableGauge<int> NumberOfCertificatesOnLastRun { get; }
    private ObservableGauge<int> NumberOfCertificatesWithTransferErrors { get; }

    public TransferAgreementAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfTransferAgreementsOnLastRun = meter.CreateObservableGauge<int>("transfer-agreements-on-last-run", () => numberOfTransferAgreementsOnLastRun);
        NumberOfCertificatesOnLastRun = meter.CreateObservableGauge<int>("certificates-on-last-run", () => numberOfCertificatesOnLastRun);
        NumberOfCertificatesWithTransferErrors = meter.CreateObservableGauge<int>("certificates-with-error", () => numberOfCertificatesWithTransferErrors);
    }

    public void SetNumberOfTransferAgreements(int transferAgreementsOnLastRun) =>
        numberOfTransferAgreementsOnLastRun = transferAgreementsOnLastRun;
    public void SetNumberOfCertificates(int certificatesOnLastRun) =>
        numberOfCertificatesOnLastRun += certificatesOnLastRun;

    public void AddTransferError() =>
        numberOfCertificatesWithTransferErrors++;
    public void ResetTransferErrors() =>
        numberOfCertificatesWithTransferErrors = 0;

    public void ResetCertificatesTransferred() =>
        numberOfCertificatesOnLastRun = 0;

    private static KeyValuePair<string, object?> CreateTag(string key, object? value) => new(key, value);
}
