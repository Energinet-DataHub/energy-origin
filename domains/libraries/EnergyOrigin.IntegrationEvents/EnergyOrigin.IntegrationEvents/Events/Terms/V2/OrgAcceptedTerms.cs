namespace EnergyOrigin.IntegrationEvents.Events.Terms.V2;
public record OrgAcceptedTerms : IntegrationEvent
{
    public Guid SubjectId { get; }
    public string? Tin { get; }
    public Guid Actor { get; }

    public OrgAcceptedTerms(Guid id, string traceId, DateTimeOffset created, Guid subjectId, string? tin, Guid actor)
        : base(id, traceId, created)
    {
        SubjectId = subjectId;
        Tin = tin;
        Actor = actor;
    }
}
