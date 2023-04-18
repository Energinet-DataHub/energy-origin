using System.ComponentModel.DataAnnotations;

namespace API.Options;

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
    [Range(typeof(bool), "false", "true")]
    public bool AllowRedirection { get; init; }
}
