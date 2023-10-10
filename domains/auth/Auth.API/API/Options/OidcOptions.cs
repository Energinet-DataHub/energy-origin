using System.ComponentModel.DataAnnotations;

namespace API.Options;

[CustomValidation(typeof(OidcOptions), "Validate")]
public class OidcOptions
{
    public const string Prefix = "Oidc";

    [Required]
    public Uri AuthorityUri { get; init; } = null!;
    [Range(typeof(TimeSpan), "00:01:00", "24:00:00")]
    public TimeSpan CacheDuration { get; init; }
    [Required]
    public string ClientId { get; init; } = null!;
    [Required]
    public string ClientSecret { get; init; } = null!;
    [Required]
    public Uri AuthorityCallbackUri { get; init; } = null!;
    [Required]
    public Uri FrontendRedirectUri { get; init; } = null!;
    public Redirection RedirectionMode { get; init; }
    public bool AllowRedirection => RedirectionMode == Redirection.Allow;
    public Generation IdGeneration { get; init; }
    public bool ReuseSubject => IdGeneration == Generation.Predictable;

    public enum Generation
    {
        Invalid, Predictable, Random
    }

    public enum Redirection
    {
        Invalid, Allow, Deny
    }

    public static ValidationResult? Validate(OidcOptions options)
    {
        if (options.IdGeneration == Generation.Invalid)
        {
            return new ValidationResult("IdGeneration is invalid");
        }
        if (options.RedirectionMode == Redirection.Invalid)
        {
            return new ValidationResult("RedirectionMode is invalid");
        }
        return ValidationResult.Success;
    }
}
