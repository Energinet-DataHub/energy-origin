using System;
using API.Shared;
using DataContext.Models;

namespace API.Transfer.Api.v2023_01_01.Dto.Responses;

public record TransferAgreementHistoryEntryDto(
    TransferAgreementDto TransferAgreement,
    long CreatedAt,
    ChangeAction Action,
    string? ActorName
);


public static class TransferAgreementHistoryEntryMapper
{
    public static TransferAgreementHistoryEntryDto ToDto(this TransferAgreementHistoryEntry historyEntry, string subject)
    {
        var changeAction = ChangeAction.Updated;
        if (historyEntry.AuditAction.Equals("Insert", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Created;
        }

        var transferAgreementDto = new TransferAgreementDto(
            historyEntry.Id,
            historyEntry.StartDate.ToUnixTimeSeconds(),
            historyEntry.EndDate?.ToUnixTimeSeconds(),
            historyEntry.SenderName,
            historyEntry.SenderTin,
            historyEntry.ReceiverTin
        );

        return new TransferAgreementHistoryEntryDto(
            transferAgreementDto,
            historyEntry.CreatedAt.ToUnixTimeSeconds(),
            changeAction,
            Guid.Parse(subject).Equals(historyEntry.SenderId) ? historyEntry.ActorName : null
        );
    }
}

