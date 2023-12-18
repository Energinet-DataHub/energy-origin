using System.ComponentModel.DataAnnotations;

namespace TransferAgreementAutomation.Worker.Options;

public class TransferApiOptions
{
    public const string TransferApi = "TransferApi";

    [Required]
    public string Url { get; set; } = string.Empty;
}
