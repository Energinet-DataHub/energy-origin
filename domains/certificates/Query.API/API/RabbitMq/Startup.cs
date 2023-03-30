using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using API.RabbitMq.Configurations;
using API.TransferCertificateHandler;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace API.RabbitMq;

public static class Startup
{
    public static void AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.RabbitMq));

        services.AddSingleton<IChannelHolder, ChannelHolder>();
        services.AddHostedService<ChannelWorker>();

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));
            o.AddConsumer<TransferCertificateConsumer>();
            o.AddConsumer<SomeMessageConsumer>();

            o.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var url = $"rabbitmq://{options.Host}:{options.Port}";

                cfg.Host(new Uri(url), h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}

public class ChannelWorker : BackgroundService
{
    private readonly ILogger<ChannelWorker> logger;
    private readonly ChannelReader<MessageWrapper<Message1>> reader;
    private readonly IBus bus;

    public ChannelWorker(IChannelHolder channelHolder, IBus bus, ILogger<ChannelWorker> logger)
    {
        this.bus = bus;
        this.logger = logger;
        reader = channelHolder.Reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var wrapper = await reader.ReadAsync(stoppingToken);
            await bus.Publish(
                new Message2 { SomeId = wrapper.Message.SomeId, SomeValue = 43},
                ctx => wrapper.SetIdsForOutgoingMessage(ctx),
                stoppingToken);

            logger.LogInformation(wrapper.Message.SomeValue);
        }
    }
}

public class MessageWrapper<TIncoming> where TIncoming : class
{
    private readonly Guid? correlationId;
    private readonly Guid? conversationId;
    private readonly Guid? initiatorId;

    public MessageWrapper(ConsumeContext<TIncoming> context)
    {
        Message = context.Message;
        correlationId = context.CorrelationId;
        conversationId = context.ConversationId;
        initiatorId = context.InitiatorId;
    }

    public TIncoming Message { get; }

    public void SetIdsForOutgoingMessage<TOutgoing>(PublishContext<TOutgoing> ctx) where TOutgoing : class
    {
        // Based on https://masstransit.io/documentation/concepts/messages#message-correlation
        ctx.ConversationId = conversationId;
        ctx.InitiatorId = correlationId;
    }
}

public interface IChannelHolder
{
    ChannelReader<MessageWrapper<Message1>> Reader { get; }
    ChannelWriter<MessageWrapper<Message1>> Writer { get; }
}

public class ChannelHolder : IChannelHolder
{
    private readonly Channel<MessageWrapper<Message1>> channel;

    public ChannelHolder() => channel = Channel.CreateUnbounded<MessageWrapper<Message1>>();

    public ChannelReader<MessageWrapper<Message1>> Reader => channel.Reader;
    public ChannelWriter<MessageWrapper<Message1>> Writer => channel.Writer;
}

public record Message1
{
    public Guid SomeId { get; init; }
    public string SomeValue { get; init; } = "";
    public Guid? CorrelationId { get; init; }
}

public record Message2
{
    public Guid SomeId { get; init; }
    public int SomeValue { get; init; }
    public Guid? CorrelationId { get; init; }
}

public record Message3
{
    public Guid SomeId { get; init; }
    public bool SomeValue { get; init; }
    public Guid? CorrelationId { get; init; }
}

public class SomeMessageConsumer : IConsumer<Message1>, IConsumer<Message2>, IConsumer<Message3>
{
    private readonly ILogger<SomeMessageConsumer> logger;
    private readonly ChannelWriter<MessageWrapper<Message1>> writer;

    public SomeMessageConsumer(ILogger<SomeMessageConsumer> logger, IChannelHolder holder)
    {
        this.logger = logger;
        writer = holder.Writer;
    }

    public async Task Consume(ConsumeContext<Message1> context)
    {
        WriteToLog(context);

        await writer.WriteAsync(new MessageWrapper<Message1>(context), context.CancellationToken);
        await context.Publish(new Message2 {SomeId = context.Message.SomeId, SomeValue = 42});
    }

    public Task Consume(ConsumeContext<Message2> context)
    {
        WriteToLog(context);

        var serializeObject = JsonConvert.SerializeObject(context);
        logger.LogInformation(serializeObject);

        return Task.CompletedTask;
        //await context.Publish(new Message3 { SomeId = context.Message.SomeId, SomeValue = true});
    }

    public Task Consume(ConsumeContext<Message3> context)
    {
        WriteToLog(context);

        return Task.CompletedTask;
    }

    private void WriteToLog<T>(ConsumeContext<T> context) where T : class =>
        logger.LogInformation("msg: {msg}. messageId: {mid}. conversationId: {conversationId}. correlationId: {correlationId}. initiatorId: {initiatorId}",
            context.Message, context.MessageId, context.ConversationId, context.CorrelationId, context.InitiatorId);
}

[ApiController]
public class MessageController : ControllerBase
{
    [HttpGet]
    [Route("api/message")]
    public async Task<IActionResult> Get([FromServices] IPublishEndpoint endpoint)
    {
        var correlationId = Guid.NewGuid();
        await endpoint.Publish(new Message1{SomeId = correlationId, SomeValue = "foo", CorrelationId = correlationId}
        //    ,
        //    ctx =>
        //{
        //    ctx.CorrelationId = correlationId;
        //}
            );

        return Ok();
    }
}
