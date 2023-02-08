namespace API.IntegrationEventBus.Configurations;

public class IntegrationEventBusOptions
{
    public const string IntegrationEventBus = "IntegrationEventBus";

    public string Url { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
