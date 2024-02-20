using MassTransit;

namespace MessageRedeliveryPoc.MassTransit;

public class MessageProducer : BackgroundService
{
    private readonly IBus bus;

    public MessageProducer(IBus bus)
    {
        this.bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // await bus.Publish(new TestMessage(), stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
