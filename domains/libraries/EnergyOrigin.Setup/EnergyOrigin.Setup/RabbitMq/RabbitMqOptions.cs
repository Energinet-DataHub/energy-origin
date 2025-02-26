using System;

namespace EnergyOrigin.Setup.RabbitMq;

public class RabbitMqOptions
{
    public const string RabbitMq = "RabbitMq";

    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int? Port { get; set; }

    public static RabbitMqOptions FromConnectionString(string connectionString)
    {
        var uri = new Uri(connectionString);
        return new RabbitMqOptions()
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = uri.UserInfo.Split(":")[0],
            Password = uri.UserInfo.Split(":")[1]
        };
    }
}
