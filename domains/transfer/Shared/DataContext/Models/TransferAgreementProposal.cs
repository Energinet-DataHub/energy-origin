using System;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class TransferAgreementProposal
{
    public Guid Id { get; set; }
    public OrganizationId SenderCompanyId { get; set; } = OrganizationId.Empty();
    public Tin SenderCompanyTin { get; set; } = Tin.Empty();
    public OrganizationName SenderCompanyName { get; set; } = OrganizationName.Empty();
    public UnixTimestamp CreatedAt { get; set; } = UnixTimestamp.Now();
    public UnixTimestamp StartDate { get; set; } = UnixTimestamp.Empty();
    public UnixTimestamp? EndDate { get; set; }
    public Tin? ReceiverCompanyTin { get; set; }
    public TransferAgreementType Type { get; set; }
    public bool IsTrial { get; set; }
}
