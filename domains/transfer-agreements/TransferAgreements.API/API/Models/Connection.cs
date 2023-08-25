using System;

namespace API.Models;

public class Connection
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationTin { get; set; }
    public Guid OwnerId { get; set; }
}
