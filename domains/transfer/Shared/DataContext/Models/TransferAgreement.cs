using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class TransferAgreement
{
    public Guid Id { get; set; }
    public UnixTimestamp StartDate { get; set; } = UnixTimestamp.Empty();
    public UnixTimestamp? EndDate { get; set; }
    public OrganizationId SenderId { get; set; } = OrganizationId.Empty();
    public OrganizationName SenderName { get; set; } = OrganizationName.Empty();
    public Tin SenderTin { get; set; } = Tin.Empty();
    public OrganizationId? ReceiverId { get; set; }
    public OrganizationName ReceiverName { get; set; } = OrganizationName.Empty();
    public Tin ReceiverTin { get; set; } = Tin.Empty();
    public Guid ReceiverReference { get; set; }
    public int TransferAgreementNumber { get; set; }
    public TransferAgreementType Type { get; set; }
}
