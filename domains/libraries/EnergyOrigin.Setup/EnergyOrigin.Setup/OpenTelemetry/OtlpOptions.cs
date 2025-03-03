using System;
using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.Setup.OpenTelemetry;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri ReceiverEndpoint { get; set; } = null!;
}
