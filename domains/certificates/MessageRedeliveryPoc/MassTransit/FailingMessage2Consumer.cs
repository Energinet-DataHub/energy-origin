using MassTransit;

namespace MessageRedeliveryPoc.MassTransit;

public class Message2Consumer : IConsumer<TestMessage2>
{
    private readonly ILogger<Message2Consumer> logger;

    public Message2Consumer(ILogger<Message2Consumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<TestMessage2> context)
    {
        logger.LogInformation("Attempt {RetryAttempt} to consume {Number}", context.GetRetryAttempt(), context.Message.Number.ToString());
        throw new Exception("Test2");
        return Task.CompletedTask;
    }
}
