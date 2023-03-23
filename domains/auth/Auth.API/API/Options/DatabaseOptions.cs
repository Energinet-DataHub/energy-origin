namespace API.Options;

public class DatabaseOptions
{
    public const string Prefix = "Database";

    public string Host { get; init; } = null!;
    public string Port { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string User { get; init; } = null!;
    public string Password { get; init; } = null!;
}
