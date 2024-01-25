namespace ClaimAutomation.Worker.Metrics;

public interface IClaimAutomationMetrics
{
    public void AddClaim();
    public void SetNumberOfCertificatesClaimed(int certificatesClaimedOnLastRun);
    public void AddClaimError();
    public void ResetClaimErrors();
    public void ResetCertificatesClaimed();
    void ResetNumberOfClaims();
}
