using System.Diagnostics.Metrics;

namespace RegistryConnector.Worker.Metrics;

public class IssuanceMetrics
{
    public const string Name = "IssuanceMeter";
    private static readonly Meter Meter = new(Name, "1.0");
    private static readonly Counter<long> IssuedCertificatesCounter = Meter.CreateCounter<long>("certificates_issued", "certificates", "Counts certificates issued");

    public static void CertificateIssued()
    {
        IssuedCertificatesCounter.Add(1);
    }
}
