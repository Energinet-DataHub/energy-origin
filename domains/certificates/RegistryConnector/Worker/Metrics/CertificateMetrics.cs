using System.Diagnostics.Metrics;

namespace RegistryConnector.Worker.Metrics;

public class CertificateMetrics
{
    private static readonly Meter Meter = new("CertificatesMeter", "1.0");
    private static readonly Counter<long> IssuedCertificatesCounter = Meter.CreateCounter<long>("certificates_issued", "certificates", "Counts certificates issued");
    private static readonly Counter<long> RejectedCertificatesCounter = Meter.CreateCounter<long>("certificates_rejected", "certificates", "Counts certificates rejected");

    public static void CertificateIssued()
    {
        IssuedCertificatesCounter.Add(1);
    }

    public static void CertificateRejected()
    {
        RejectedCertificatesCounter.Add(1);
    }
}
