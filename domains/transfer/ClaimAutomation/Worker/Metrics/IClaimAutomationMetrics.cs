namespace ClaimAutomation.Worker.Metrics;

public interface IClaimAutomationMetrics
{
    public void AddClaim();
    public void SetNumberOfCertificatesClaimed(int certificatesClaimed);
    public void AddClaimError();
    public void ResetClaimErrors();
    public void ResetCertificatesClaimed();
    void ResetNumberOfClaims();
}
