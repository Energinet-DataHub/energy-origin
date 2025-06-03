using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Processing;

public interface IMunicipalityPercentageProcessor
{
    List<MunicipalityDistribution> Calculate(IReadOnlyList<Claim> claims);
}

public sealed class MunicipalityPercentageProcessor : IMunicipalityPercentageProcessor
{
    public List<MunicipalityDistribution> Calculate(IReadOnlyList<Claim> claims)
    {
        if (!claims.Any()) return [];

        var totalQuantity = claims.Sum(x => x.Quantity);

        var grpd = from c in claims
                   group (double)c.Quantity by c.ProductionCertificate.Attributes.FirstOrDefault(y => y.Key == "municipality_code").Value
            into g
                   select new { g.Key, sum = g.Sum(x => x) };

        var municipalities = new List<MunicipalityDistribution>();
        foreach (var municipality in grpd)
        {
            var percentage = municipality.sum / totalQuantity;

            var md = new MunicipalityDistribution(municipality.Key, percentage);
            municipalities.Add(md);
        }

        return municipalities;
    }
}
