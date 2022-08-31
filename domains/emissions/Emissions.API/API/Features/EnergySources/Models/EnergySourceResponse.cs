namespace API.EnergySources.Models;

public record EnergySourceResponse(IEnumerable<EnergySourceDeclaration> EnergySources);
