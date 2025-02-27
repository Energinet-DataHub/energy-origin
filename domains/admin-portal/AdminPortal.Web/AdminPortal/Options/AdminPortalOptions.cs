using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Options;

public class AdminPortalOptions
{
    public const string Prefix = "AdminPortal";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string TenantId { get; set; } = null!;

    [Required]
    public string Scope { get; set; } = null!;
}
