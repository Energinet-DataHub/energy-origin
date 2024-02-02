using System.ComponentModel.DataAnnotations;

namespace ClaimAutomation.Worker.Options;

public class DatabaseOptions
{
    public const string Prefix = "Database";
    [Required]
    public string Host { get; set; } = string.Empty;
    [Required]
    public string Port { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string User { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public string ToConnectionString()
        => $"Host={Host}; Port={Port}; Database={Name}; Username={User}; Password={Password};";
}
