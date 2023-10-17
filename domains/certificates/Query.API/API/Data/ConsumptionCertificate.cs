using CertificateValueObjects;

namespace API.Data;

public class ConsumptionCertificate : Certificate
{
    private ConsumptionCertificate() { }

    public ConsumptionCertificate(string gridArea, Period period, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
        : base(gridArea, period, meteringPointOwner, gsrn, quantity, blindingValue)
    { }
}
