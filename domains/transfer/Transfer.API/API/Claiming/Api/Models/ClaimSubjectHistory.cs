using System;

namespace API.Claiming.Api.Models;

public record ClaimSubjectHistory
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string ActorId { get; set; } = String.Empty;
    public string ActorName { get; set; } = String.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string AuditAction { get; set; } = String.Empty;
}
