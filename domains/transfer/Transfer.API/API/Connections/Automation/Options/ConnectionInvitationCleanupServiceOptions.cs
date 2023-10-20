using System;
using System.ComponentModel.DataAnnotations;

namespace API.Connections.Automation.Options;

public class ConnectionInvitationCleanupServiceOptions
{
    public const string Prefix = "ConnectionInvitationCleanupService";

    [Range(typeof(TimeSpan), "00:00:02", "02:00:00")]
    [Required]
    public TimeSpan SleepTime { get; set; }
}
