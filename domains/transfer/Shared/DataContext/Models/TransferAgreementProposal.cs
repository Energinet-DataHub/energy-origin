using System;
using System.ComponentModel.DataAnnotations.Schema;
using EnergyOrigin.Domain.ValueObjects;

namespace DataContext.Models;

public class TransferAgreementProposal
{
    public Guid Id { get; set; }
    public Guid SenderCompanyId { get; set; }
    public Tin SenderCompanyTin { get; set; } = Tin.Empty();
    public string SenderCompanyName { get; set; } = string.Empty;
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Tin? ReceiverCompanyTin { get; set; }
}
