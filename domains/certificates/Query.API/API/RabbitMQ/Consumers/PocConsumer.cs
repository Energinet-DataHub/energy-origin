using System.Threading.Tasks;
using API.RabbitMQ.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.RabbitMQ.Consumers
{
    public class PocConsumer :
        IConsumer<HelloMessage>
    {
        readonly ILogger<PocConsumer> logger;

        public PocConsumer(ILogger<PocConsumer> logger)
        {
            this.logger = logger;
        }
        public Task Consume(ConsumeContext<HelloMessage> context)
        {
            logger.LogInformation("Hello {Name}", context.Message.Name);

            return Task.CompletedTask;
        }
    }
}
