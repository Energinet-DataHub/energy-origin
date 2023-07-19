using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class TermsOptions
{
    public const string Prefix = "Terms";

    [Required]
    public string PrivacyPolicyVersion { get; init; } = null!;
    [Required]
    public string TermsOfServiceVersion { get; init; } = null!;
}
