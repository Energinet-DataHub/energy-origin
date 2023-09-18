using System.ComponentModel.DataAnnotations;

namespace API.Cvr;

public class CvrOptions
{
    public const string Prefix = "Cvr";

    [Required]
    public string User { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
