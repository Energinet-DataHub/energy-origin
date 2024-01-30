namespace ClaimAutomation.Worker.Metrics;

public interface IClaimAutomationMetrics
{
    public void AddClaim();
    void AddCertificateClaimedThisRun();
    public void AddClaimError();
    public void ResetClaimErrors();
    public void ResetCertificatesClaimed();
    void ResetNumberOfClaims();
}
