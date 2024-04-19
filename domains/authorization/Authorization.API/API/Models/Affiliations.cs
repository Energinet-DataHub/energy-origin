namespace API.Models;

public class Affiliation
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
}
