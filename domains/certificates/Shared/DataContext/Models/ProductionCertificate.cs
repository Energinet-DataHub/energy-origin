using DataContext.ValueObjects;

namespace DataContext.Models;

public class ProductionCertificate : Certificate
{
    private ProductionCertificate()
    {
    }

    public ProductionCertificate(string gridArea, Period period, Technology technology, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
        : base(gridArea, period, meteringPointOwner, gsrn, quantity, blindingValue)
    {
        Technology = technology;
    }
    public Technology Technology { get; private set; } = new("unknown", "unknown");
}
