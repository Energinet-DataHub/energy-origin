namespace API.RabbitMq.Configurations;

public class RabbitMqOptions
{
    public const string RabbitMq = "RabbitMq";

    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
}
