namespace API.EnergySources.Models;

public record EnergySourceDeclaration(long DateFrom, long DateTo, decimal Renewable, Dictionary<string, decimal> Ratios);
