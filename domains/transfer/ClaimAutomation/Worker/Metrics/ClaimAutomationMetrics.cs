using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

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

        NumberOfClaimsOnLastRun = meter.CreateObservableGauge<int>("ett_claim_automation_claims_on_last_run", () => numberOfClaimsOnLastRun);
        TotalClaimedCertificatesOnLastRun = meter.CreateObservableGauge<int>("ett_claim_automation_total_claimed_certificates_on_last_run", () => totalClaimedCertificatesOnLastRun);
        NumberOfCertificatesWithClaimErrors = meter.CreateObservableGauge<int>("ett_claim_automation_certificates_with_claim_error", () => numberOfCertificatesWithClaimErrors);
    }

    public void AddClaim() =>
        numberOfClaimsOnLastRun++;

    public void AddCertificateClaimedThisRun() =>
        totalClaimedCertificatesOnLastRun++;

    public void AddClaimError() =>
        numberOfCertificatesWithClaimErrors++;
    public void ResetClaimErrors() =>
        numberOfCertificatesWithClaimErrors = 0;

    public void ResetCertificatesClaimed() =>
        totalClaimedCertificatesOnLastRun = 0;

    public void ResetNumberOfClaims() =>
        numberOfClaimsOnLastRun = 0;
}
