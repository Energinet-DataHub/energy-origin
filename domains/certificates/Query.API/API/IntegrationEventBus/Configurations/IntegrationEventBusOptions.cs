namespace API.IntegrationEventBus.Configurations;

public class IntegrationEventBusOptions
{
    public const string IntegrationEventBus = "RabbitMq";

    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
}
