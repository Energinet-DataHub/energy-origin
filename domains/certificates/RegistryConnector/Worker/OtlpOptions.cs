using System;
using System.ComponentModel.DataAnnotations;

namespace RegistryConnector.Worker;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri ReceiverEndpoint { get; set; } = null!;
}
