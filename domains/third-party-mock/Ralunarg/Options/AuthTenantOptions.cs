using System.ComponentModel.DataAnnotations;

namespace Ralunarg.Options;

public class AuthTenantOptions
{
    public const string AuthTenant = nameof(AuthTenant);

    [Required]
    public string ClientId { get; set; } = "";
    [Required]
    public string ClientSecret { get; set; } = "";
    [Required]
    public string Scope { get; set; } = "";
}
