using System;
using System.ComponentModel.DataAnnotations;

namespace ClaimAutomation.Worker.Options;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri ReceiverEndpoint { get; set; } = null!;

    [Required]
    public bool Enabled { get; set; }
}
