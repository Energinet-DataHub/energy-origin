using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient.Models;

namespace Worker.UnitTests;

public static class Any
{
    public static GranularCertificate GranularCertificate(UnixTimestamp from, CertificateType certificateType, uint quantity = 10, bool isTrial = false)
    {
        return new GranularCertificate()
        {
            FederatedStreamId = new FederatedStreamId() { Registry = "123", StreamId = Guid.NewGuid() },
            Start = from.EpochSeconds,
            End = from.AddHours(1).EpochSeconds,
            CertificateType = certificateType,
            GridArea = "DK1",
            Quantity = quantity,
            Attributes = new Dictionary<string, string>()
            {
                {"IsTrial", isTrial.ToString()}
            }
        };
    }

    public static GranularCertificate GranularCertificate()
    {
        return GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production);
    }

    public static List<GranularCertificate> GranularCertificatesList(int count)
    {
        return Enumerable.Range(0, count).Select(_ => GranularCertificate()).ToList();
    }

    public static List<GranularCertificate> GranularCertificatesList(int count, UnixTimestamp from)
    {
        return Enumerable.Range(0, count).Select(_ => GranularCertificate(from, CertificateType.Production)).ToList();
    }

    public static OrganizationId OrganizationId()
    {
        return EnergyOrigin.Domain.ValueObjects.OrganizationId.Create(Guid.NewGuid());
    }
}
