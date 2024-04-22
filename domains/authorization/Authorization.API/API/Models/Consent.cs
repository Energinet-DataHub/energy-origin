using System;

namespace API.Models;

public class Consent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = new();
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = new();
    public DateTime ConsentDate { get; set; }
    public ConsentStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
