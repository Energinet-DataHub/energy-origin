using MassTransit;

namespace MessageRedeliveryPoc.MassTransit;

public class MessageConsumer : IConsumer<TestMessage>
{
    private readonly ILogger<MessageConsumer> logger;

    public MessageConsumer(ILogger<MessageConsumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<TestMessage> context)
    {
        logger.LogInformation("Attempt {RetryAttempt} to consume {Id}", context.GetRetryAttempt(), context.Message.Id.ToString());
        throw new Exception("Test");
        return Task.CompletedTask;
    }
}
