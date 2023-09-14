using System;
using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class ConnectionInvitationCleanupServiceOptions
{
    public const string Prefix = "ConnectionInvitationCleanupService";

    [Range(typeof(TimeSpan), "00:00:02", "24:00:00")]
    public TimeSpan SleepTime { get; set; }
}
