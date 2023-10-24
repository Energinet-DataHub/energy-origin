using System;

namespace API.Claiming.Api.Models;

public record ClaimSubjectHistory
{
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string AuditAction { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
