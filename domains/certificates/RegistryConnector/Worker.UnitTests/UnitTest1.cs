using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

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

public class UnitTest1
{
    private record SomeIncomingMessage
    {
        public Guid SomeId { get; init; }
        public string SomeValue { get; init; } = "";
    }

    private record SomeOutgoingMessage
    {
        public Guid SomeId { get; init; }
        public int SomeValue { get; init; }
    }

    //private class MessageWrapperList
    //{
    //    private readonly List<MessageWrapper<SomeIncomingMessage>> wrappers = new();

    //    public void Add(MessageWrapper<SomeIncomingMessage> wrapper) => wrappers.Add(wrapper);

    //    public IReadOnlyList<MessageWrapper<SomeIncomingMessage>> Get() => wrappers;
    //}

    private class SomeMessageConsumer : IConsumer<SomeIncomingMessage>, IConsumer<SomeOutgoingMessage>
    {
        public Task Consume(ConsumeContext<SomeIncomingMessage> context) => Task.CompletedTask;

        public Task Consume(ConsumeContext<SomeOutgoingMessage> context) => Task.CompletedTask;
    }

    private class ReceiveObserver : IReceiveObserver
    {
        public MessageWrapper<SomeIncomingMessage> WrappedIncomingMessage { get; set; }
        public Guid? IncomingConversationId { get; set; }
        public Guid? IncomingCorrelationId { get; set; }
        public Guid? IncomingInitiatorId { get; set; }

        public Guid? OutgoingConversationId { get; private set; }
        public Guid? OutgoingCorrelationId { get; private set; }
        public Guid? OutgoingInitiatorId { get; private set; }

        public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            if (context.Message.GetType() == typeof(SomeIncomingMessage))
            {
                WrappedIncomingMessage = new MessageWrapper<SomeIncomingMessage>((ConsumeContext<SomeIncomingMessage>)context);

                IncomingConversationId = context.ConversationId;
                IncomingInitiatorId = context.InitiatorId;
                IncomingCorrelationId = context.CorrelationId;
            }

            if (context.Message.GetType() == typeof(SomeOutgoingMessage))
            {
                OutgoingConversationId = context.ConversationId;
                OutgoingInitiatorId = context.InitiatorId;
                OutgoingCorrelationId = context.CorrelationId;
            }
            
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context) => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception) => Task.CompletedTask;
    }

    [Fact]
    public async Task ids_are_correctly_set_for_a_message_without_correlation_id()
    {
        var receiveObserver = new ReceiveObserver();

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddReceiveObserver(_ => receiveObserver);
                cfg.AddConsumer<SomeMessageConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(new SomeIncomingMessage { SomeId = Guid.NewGuid(), SomeValue = "foo"});

        (await harness.Consumed.Any<SomeIncomingMessage>()).Should().BeTrue();

        var wrapper = receiveObserver.WrappedIncomingMessage;

        await harness.Bus.Publish(
            new SomeOutgoingMessage { SomeId = wrapper.Message.SomeId, SomeValue = 42 },
            ctx => wrapper.SetIdsForOutgoingMessage(ctx));

        (await harness.Consumed.Any<SomeOutgoingMessage>()).Should().BeTrue();

        receiveObserver.OutgoingConversationId.Should().Be(receiveObserver.IncomingConversationId);
        receiveObserver.OutgoingInitiatorId.Should().BeNull();
        receiveObserver.OutgoingCorrelationId.Should().BeNull();
    }
}
