namespace EnergyOrigin.IntegrationEvents.Events;

public record OrgAcceptedTerms(Guid SubjectId, string Tin, Guid Actor);
