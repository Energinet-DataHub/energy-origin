using System.ComponentModel.DataAnnotations;
using System;

namespace API.Options;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri ReceiverEndpoint { get; init; } = null!;
}
