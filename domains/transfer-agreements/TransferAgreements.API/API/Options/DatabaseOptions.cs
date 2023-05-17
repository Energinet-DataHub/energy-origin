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

    public string ToConnectionString()
        => $"Host={Host}; Port={Port}; Database={Name}; Username={User}; Password={Password};";
}
