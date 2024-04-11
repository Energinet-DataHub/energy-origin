using System.ComponentModel.DataAnnotations;
using System;

namespace API.IssuingContractCleanup;

public class IssuingContractCleanupOptions
{
    public const string Prefix = "IssuingContractCleanup";

    [Range(typeof(TimeSpan), "00:00:02", "02:00:00")]
    [Required]
    public TimeSpan SleepTime { get; set; }
}
