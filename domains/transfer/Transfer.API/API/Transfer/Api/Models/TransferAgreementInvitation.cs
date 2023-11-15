using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Transfer.Api.Models;

public class TransferAgreementInvitation
{
    public Guid Id { get; set; }
    public Guid SenderCompanyId { get; set; }
    public string SenderCompanyTin { get; set; } = string.Empty;
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string ReceiverCompanyTin { get; set; } = string.Empty;
}
