using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class TransferAgreement
{
    public Guid Id { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Tin SenderTin { get; set; } = Tin.Empty();
    public string ReceiverName { get; set; } = string.Empty;
    public Tin ReceiverTin { get; set; } = Tin.Empty();
    public Guid ReceiverReference { get; set; }
    public int TransferAgreementNumber { get; set; }
}
