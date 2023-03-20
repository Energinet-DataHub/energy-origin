namespace API.Options;

public class JaegerOptions
{
    public const string Prefix = "Jaeger";

    public string AgentHost { get; init; } = null!;
    public int AgentPort { get; init; }
}
