using System.ComponentModel.DataAnnotations;

namespace ClaimAutomation.Worker.Options;

public class ClaimAutomationOptions
{
    public const string Prefix = "ClaimAutomation";

    [Required]
    public bool Enabled { get; set; }
}
