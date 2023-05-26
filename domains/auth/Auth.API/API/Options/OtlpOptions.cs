using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public string ReceiverEndpoint { get; init; } = null!;
}
