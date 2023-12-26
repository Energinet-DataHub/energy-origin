namespace MassTransitContracts.Contracts;

public class UserActivityEvent
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public EntityType EntityType { get; set; }
    public DateTimeOffset ActivityDate { get; set; }
    public Guid OrganizationId { get; set; }
    public string Tin { get; set; } = "";
    public string OrganizationName { get; set; } = "";
}

