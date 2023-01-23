namespace Consumers
{
    using System.Threading.Tasks;
    using MassTransit;
    using Contracts;
    using Microsoft.Extensions.Logging;

    public class PocConsumer :
        IConsumer<HelloMessage>
    {
        readonly ILogger<PocConsumer> _logger;

        public PocConsumer(ILogger<PocConsumer> logger)
        {
            _logger = logger;
        }
        public Task Consume(ConsumeContext<HelloMessage> context)
        {
            _logger.LogInformation("Hello {Name}", context.Message.Name);

            return Task.CompletedTask;
        }
    }
}
