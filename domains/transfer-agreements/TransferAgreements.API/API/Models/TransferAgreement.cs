using System;
using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class TransferAgreement
{
    public Guid Id { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }
    public string SenderTin { get; set; }
    public string ReceiverTin { get; set; }
    public Guid ReceiverReference { get; set; }
    public int TransferAgreementNumber { get; set; }
}
