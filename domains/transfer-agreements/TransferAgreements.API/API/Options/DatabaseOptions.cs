
namespace API.Options;

public class DatabaseOptions
{
    public const string Prefix = "Database";

    public string Host { get; set; } = string.Empty;

    public string Port { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ToConnectionString()
        => $"Host={Host}; Port={Port}; Database={Name}; Username={User}; Password={Password};";
}
