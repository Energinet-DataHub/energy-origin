namespace API.Models;

public class Consent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; }
    public DateTime ConsentDate { get; set; }
}
