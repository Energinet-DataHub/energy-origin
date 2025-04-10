using System.ComponentModel.DataAnnotations;

namespace API.Configurations;

public class StampOptions
{
    public const string Stamp = nameof(Stamp);

    [Required]
    public string Url { get; set; } = "";

    [Required]
    public string RegistryName { get; set; } = "";
}
