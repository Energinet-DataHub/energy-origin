using System.Diagnostics.Metrics;

namespace ClaimAutomation.Worker.Metrics;
public class ClaimAutomationMetrics : IClaimAutomationMetrics
{
    public const string MetricName = "ClaimAutomation";

    private int numberOfClaimsOnLastRun = 0;
    private int totalClaimedCertificatesOnLastRun = 0;
    private int numberOfCertificatesWithClaimErrors = 0;
    private ObservableGauge<int> NumberOfClaimsOnLastRun { get; }
    private ObservableGauge<int> TotalClaimedCertificatesOnLastRun { get; }
    private ObservableGauge<int> NumberOfCertificatesWithClaimErrors { get; }

    public ClaimAutomationMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfClaimsOnLastRun = meter.CreateObservableGauge<int>("claims-on-last-run", () => numberOfClaimsOnLastRun);
        TotalClaimedCertificatesOnLastRun = meter.CreateObservableGauge<int>("total-claimed-certificates-on-last-run", () => totalClaimedCertificatesOnLastRun);
        NumberOfCertificatesWithClaimErrors = meter.CreateObservableGauge<int>("certificates-with-claim-error", () => numberOfCertificatesWithClaimErrors);
    }

    public void AddClaim() =>
        numberOfClaimsOnLastRun++;
    public void SetNumberOfCertificatesClaimed(int certificatesClaimedOnLastRun) =>
        totalClaimedCertificatesOnLastRun += certificatesClaimedOnLastRun;

    public void AddClaimError() =>
        numberOfCertificatesWithClaimErrors++;
    public void ResetClaimErrors() =>
        numberOfCertificatesWithClaimErrors = 0;

    public void ResetCertificatesClaimed() =>
        totalClaimedCertificatesOnLastRun = 0;

    public void ResetNumberOfClaims() =>
        numberOfClaimsOnLastRun = 0;
}
