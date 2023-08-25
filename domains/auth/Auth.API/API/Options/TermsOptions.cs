using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class TermsOptions
{
    public const string Prefix = "Terms";

    [Required, Range(1, int.MaxValue)]
    public int PrivacyPolicyVersion { get; init; }
    [Required, Range(1, int.MaxValue)]
    public int TermsOfServiceVersion { get; init; }
}
