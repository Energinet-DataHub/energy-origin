using System;
using API.Shared;
using API.Transfer.Api.Models;

namespace API.Transfer.Api.v2023_11_11.Dto.Responses;

public record TransferAgreementHistoryEntryDto(
    v2023_01_01.Dto.Responses.TransferAgreementDto TransferAgreement,
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

        var transferAgreementDto = new v2023_01_01.Dto.Responses.TransferAgreementDto(
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

