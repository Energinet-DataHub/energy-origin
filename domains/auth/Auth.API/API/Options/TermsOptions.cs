using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class TermsOptions
{
    public const string Prefix = "Terms";

    [Required]
    public int PrivacyPolicyVersion { get; init; }
    [Required]
    public int TermsOfServiceVersion { get; init; }
}
