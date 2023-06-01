using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class DatabaseOptions
{
    public const string Prefix = "Database";

    [Required] public string Host { get; set; } = null!;

    [Required] public string Port { get; set; } = null!;

    [Required] public string Name { get; set; } = null!;

    [Required] public string User { get; set; } = null!;

    [Required] public string Password { get; set; } = null!;

    public string ToConnectionString()
        => $"Host={Host}; Port={Port}; Database={Name}; Username={User}; Password={Password};";
}
