using System;
using System.Text.Json.Serialization;
using API.Data;

namespace API.ApiModels.Responses;

public record TransferAgreementAuditDto(
    string ActorId,
    string? ActorName,
    long AuditDate,
    ChangeAction Action,
    TransferAgreementDto TransferAgreement);

   public enum ChangeAction {Created = 1, Updated = 2, Deleted = 3}

public static class TransferAgreementAuditMapper
{
    public static TransferAgreementAuditDto ToDto(this TransferAgreementAudit audit, TransferAgreementDto transferAgreement, string subject)
    {
        var changeAction = ChangeAction.Updated;
        if (audit.AuditAction.Equals("Inserted", StringComparison.InvariantCultureIgnoreCase))
        {
            changeAction = ChangeAction.Created;
        }

        return new TransferAgreementAuditDto(
            audit.ActorId,
            Guid.Parse(subject).Equals(audit.TransferAgreement.SenderId) ? audit.ActorName : null,
            new DateTimeOffset(audit.AuditDate).ToUnixTimeSeconds(),
            changeAction,
            transferAgreement);
    }

}
