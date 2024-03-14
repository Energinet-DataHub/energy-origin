using System.ComponentModel.DataAnnotations;
using System;

namespace API.Transfer.TransferAgreementProposalCleanup.Options;
public class TransferAgreementProposalCleanupServiceOptions
{
    public const string Prefix = "TransferAgreementProposalCleanupService";

    [Range(typeof(TimeSpan), "00:00:02", "02:00:00")]
    [Required]
    public TimeSpan SleepTime { get; set; }
}
