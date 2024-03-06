namespace EnergyOrigin.IntegrationEvents.Events.Terms.V1;

public record OrgAcceptedTerms(Guid SubjectId, string Tin, Guid Actor);
