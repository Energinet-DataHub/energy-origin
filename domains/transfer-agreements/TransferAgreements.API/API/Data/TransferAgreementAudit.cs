using System;

namespace API.Data;

public class TransferAgreementAudit
{
    public Guid Id { get; set; }
    public string ActorId { get; set; }
    public string ActorName { get; set; }
    public DateTime AuditDate { get; set; }
    public string AuditAction { get; set; }
    public TransferAgreement TransferAgreement { get; set; }
}
