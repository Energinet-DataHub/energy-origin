using System;

namespace API.Models;

public class AuditRecord
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Ensuring default is empty string if not set
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Default to current time
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
}
