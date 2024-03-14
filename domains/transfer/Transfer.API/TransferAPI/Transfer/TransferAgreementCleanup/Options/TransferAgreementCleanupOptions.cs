using System.ComponentModel.DataAnnotations;
using System;

namespace API.Transfer.TransferAgreementCleanup.Options;

public class TransferAgreementCleanupOptions
{
    public const string Prefix = "TransferAgreementCleanup";

    [Range(typeof(TimeSpan), "00:00:02", "02:00:00")]
    [Required]
    public TimeSpan SleepTime { get; set; }
}
