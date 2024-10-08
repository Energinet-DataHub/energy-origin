using System.ComponentModel.DataAnnotations;

namespace ClaimAutomation.Worker.Options;

public class ClaimAutomationOptions
{
    public const string Prefix = "ClaimAutomation";

    [Required]
    public bool Enabled { get; set; }

    public int CertificateFetchBachSize { get; set; } = 1000;

    public ScheduleInterval ScheduleInterval { get; set; } = ScheduleInterval.EveryHourHalfPast;
}

public enum ScheduleInterval { EveryHourHalfPast, Every5Seconds }
