using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing;

namespace EnergyOrigin.ActivityLog;

public class ActivityLogOptions
{
    public const string Prefix = "ActivityLog";

    [Required]
    public string? ServiceName { get; set; }
    public int CleanupActivityLogsOlderThanInDays { get; set; } = 60;
    public int CleanupIntervalInSeconds { get; set; } = 15 * 60; // 15 minutes
}
