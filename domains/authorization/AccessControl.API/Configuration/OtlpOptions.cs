using System;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.API.Configuration;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required] public Uri ReceiverEndpoint { get; set; } = null!;
}
