using System;
using API.Claiming.Api.Models;
using API.Shared;

namespace API.Claiming.Api.Dto.Response;

public record ClaimSubjectHistoryEntryDto(
    long CreatedAt,
    ChangeAction Action,
    string? ActorName
);

public static class ClaimSubjectHistoryEntryMapper
{
    public static ClaimSubjectHistoryEntryDto ToDto(this ClaimSubjectHistory historyEntry)
    {
        var changeAction = ChangeAction.Updated;

        if (historyEntry.AuditAction.Equals("Insert", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Created;
        }

        if (historyEntry.AuditAction.Equals("Delete", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Deleted;
        }

        return new ClaimSubjectHistoryEntryDto(
            historyEntry.CreatedAt.ToUnixTimeSeconds(),
            changeAction,
            historyEntry.ActorName
        );
    }
}
