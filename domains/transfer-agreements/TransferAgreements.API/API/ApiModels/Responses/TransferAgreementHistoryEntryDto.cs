using System;
using System.Text.Json.Serialization;
using API.Data;

namespace API.ApiModels.Responses;

public record TransferAgreementHistoryEntryDto(
    long StartDate,
    long? EndDate,
    string? SenderName,
    string? ActorName,
    string SenderTin,
    string ReceiverTin,
    long AuditDate,
    ChangeAction Action);


   public enum ChangeAction {Created = 1, Updated = 2, Deleted = 3}

public static class TransferAgreementHistoryEntryMapper
{
    public static TransferAgreementHistoryEntryDto ToDto(this TransferAgreementHistoryEntry historyEntry, string subject)
    {
        var changeAction = ChangeAction.Updated;
        if (historyEntry.AuditAction.Equals("Insert", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Created;
        }

        return new TransferAgreementHistoryEntryDto(
            historyEntry.StartDate.ToUnixTimeSeconds(),
            historyEntry.EndDate?.ToUnixTimeSeconds(),
            historyEntry.SenderName,
            Guid.Parse(subject).Equals(historyEntry.SenderId) ? historyEntry.ActorName : null,
            historyEntry.SenderTin,
            historyEntry.ReceiverTin,
            new DateTimeOffset(historyEntry.AuditDate).ToUnixTimeSeconds(),
         changeAction);

    }
}
