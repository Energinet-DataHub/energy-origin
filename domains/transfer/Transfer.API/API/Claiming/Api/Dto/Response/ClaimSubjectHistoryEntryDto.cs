using System;

namespace API.Claiming.Api.Dto.Response;

public record ClaimSubjectHistoryEntryDto
{
    public string ActorName { get; set; } = String.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string AuditAction { get; set; } = String.Empty;
}
