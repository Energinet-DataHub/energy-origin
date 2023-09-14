using System;
using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class ConnectionInvitationCleanupServiceOptions
{
    public const string Prefix = "ConnectionInvitationCleanupService";

    [Required]
    public TimeSpan SleepTime { get; set; }
}
