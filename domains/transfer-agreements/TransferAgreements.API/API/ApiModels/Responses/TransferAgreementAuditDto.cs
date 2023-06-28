using System;
using System.Text.Json.Serialization;
using API.Data;

namespace API.ApiModels.Responses;

public record TransferAgreementAuditDto(
    long? EndDate,
    string? SenderName,
    string? ActorName,
    string SenderTin,
    string ReceiverTin,
    long AuditDate,
    ChangeAction Action);


   public enum ChangeAction {Created = 1, Updated = 2, Deleted = 3}

public static class TransferAgreementAuditMapper
{
    public static TransferAgreementAuditDto ToDto(this TransferAgreementAudit audit, string subject)
    {
        var changeAction = ChangeAction.Updated;
        if (audit.AuditAction.Equals("Inserted", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Created;
        }

        return new TransferAgreementAuditDto(
            audit.EndDate?.ToUnixTimeSeconds(),
            audit.SenderName,
            Guid.Parse(subject).Equals(audit.SenderId) ? audit.ActorName : null,
            audit.SenderTin,
            audit.ReceiverTin,
            new DateTimeOffset(audit.AuditDate).ToUnixTimeSeconds(),
         changeAction);

    }
}
