using System;

namespace API.Data;

public class TransferAgreementHistoryEntry
{
    public Guid Id { get; set; }
    public Guid TransferAgreementId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string ActorId { get; set; }
    public string ActorName { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public string SenderTin { get; set; }
    public string ReceiverTin { get; set; }
    public DateTimeOffset AuditDate { get; set; }
    public string AuditAction { get; set; }
    public TransferAgreement TransferAgreement { get; set; }
}
