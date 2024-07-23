using MassTransit;

namespace API;

public class ConfigureQuorumReceiveEndpoint : IConfigureReceiveEndpoint
{
    private const string QueueType = "x-queue-type";
    private const string QuorumQueue = "quorum";

    /// <summary>
    /// Configures the receive endpoint. The given endpoint will be defined as a quorum in RabbitMQ.
    /// </summary>
    public void Configure(string name, IReceiveEndpointConfigurator configurator)
    {
        if (configurator is IRabbitMqReceiveEndpointConfigurator rabbitMqConfigurator)
        {
            rabbitMqConfigurator.SetQueueArgument(QueueType, QuorumQueue);
        }
    }
}
