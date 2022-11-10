using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class EnergyMeasConsumer : IConsumer<Measurement>
{
    private readonly ILogger<EnergyMeasConsumer> logger;

    public EnergyMeasConsumer(ILogger<EnergyMeasConsumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<Measurement> context)
    {
        logger.LogInformation("Got {meas}", context.Message);
        return Task.CompletedTask;
    }
}
