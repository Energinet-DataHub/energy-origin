using System.ComponentModel.DataAnnotations;

namespace API.Cvr.Api.Clients.Cvr;

public class CvrOptions
{
    public const string Prefix = "Cvr";

    [Required]
    public string User { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string BaseUrl { get; set; } = string.Empty;
}
