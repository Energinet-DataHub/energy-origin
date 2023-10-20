using System;

namespace API.Transfer.Api.Models;

public class TransferAgreementHistoryEntry
{
    public Guid Id { get; set; }
    public Guid TransferAgreementId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string ActorId { get; set; } = String.Empty;
    public string ActorName { get; set; } = String.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = String.Empty;
    public string SenderTin { get; set; } = String.Empty;
    public string ReceiverTin { get; set; } = String.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string AuditAction { get; set; } = String.Empty;
    public TransferAgreement TransferAgreement { get; set; } = null!;
}
