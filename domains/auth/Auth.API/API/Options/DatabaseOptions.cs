using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class DatabaseOptions
{
    public const string Prefix = "Database";

    [Required]
    public string Host { get; init; } = null!;
    [Required]
    public string Port { get; init; } = null!;
    [Required]
    public string Name { get; init; } = null!;
    [Required]
    public string User { get; init; } = null!;
    [Required]
    public string Password { get; init; } = null!;
}
