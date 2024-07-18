using System.ComponentModel.DataAnnotations;

namespace TransferAgreementAutomation.Worker.Options;

public class TransferAgreementAutomationOptions
{
    public const string Prefix = "TransferAgreementAutomation";

    [Required]
    public bool Enabled { get; set; }
}
