using System;

namespace API.Models;

public class Connection
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyTin { get; set; }
    public Guid OwnerId { get; set; }
}
