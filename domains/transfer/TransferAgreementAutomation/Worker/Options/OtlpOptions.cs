using System;
using System.ComponentModel.DataAnnotations;

namespace TransferAgreementAutomation.Worker.Options;

public class OtlpOptions
{
    public const string Prefix = "Otlp";

    [Required]
    public Uri ReceiverEndpoint { get; set; } = null!;
}
