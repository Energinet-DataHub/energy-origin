using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class TermsOptions
{
    public const string Prefix = "Terms";

    [Range(1, int.MaxValue)]
    public required int CurrentVersion { get; init; }
}
