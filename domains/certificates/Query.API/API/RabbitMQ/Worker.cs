using System.Threading;
using System.Threading.Tasks;
using API.RabbitMQ.Contracts;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace API.RabbitMQ
{
    public class Worker : BackgroundService
    {
        readonly IBus _bus;

        public Worker(IBus bus)
        {
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                await _bus.Publish(new HelloMessage
                {
                    Name = "World"
                }, stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }

        }
    }
}
