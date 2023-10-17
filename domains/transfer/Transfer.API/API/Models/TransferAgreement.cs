using System;

namespace API.Models;

public class TransferAgreement
{
    public Guid Id { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderTin { get; set; } = string.Empty;
    public string ReceiverTin { get; set; } = string.Empty;
    public Guid ReceiverReference { get; set; }
    public int TransferAgreementNumber { get; set; }
}
