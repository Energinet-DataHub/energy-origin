using System.Threading.Tasks;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Consumers
{
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
