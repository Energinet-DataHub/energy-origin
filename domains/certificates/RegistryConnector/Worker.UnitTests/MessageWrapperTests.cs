using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

public class MessageWrapperTests
{
    [Fact]
    public async Task ids_are_set_for_a_message_without_correlation_id()
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

        await harness.Consumed.Any<SomeIncomingMessage>();

        var wrapper = receiveObserver.WrappedIncomingMessage;

        await harness.Bus.Publish(
            new SomeOutgoingMessage { SomeId = wrapper.Message.SomeId, SomeValue = 42 },
            ctx => wrapper.SetIdsForOutgoingMessage(ctx));

        await harness.Consumed.Any<SomeOutgoingMessage>();

        receiveObserver.OutgoingConversationId.Should().Be(receiveObserver.IncomingConversationId);
        receiveObserver.OutgoingInitiatorId.Should().BeNull();
        receiveObserver.OutgoingCorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task ids_are_set_for_a_message_with_correlation_id()
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

        var correlationId = Guid.NewGuid();

        await harness.Bus.Publish(new SomeIncomingMessage { SomeId = Guid.NewGuid(), SomeValue = "foo" }, ctx => ctx.CorrelationId = correlationId);

        await harness.Consumed.Any<SomeIncomingMessage>();

        var wrapper = receiveObserver.WrappedIncomingMessage;

        await harness.Bus.Publish(
            new SomeOutgoingMessage { SomeId = wrapper.Message.SomeId, SomeValue = 42 },
            ctx => wrapper.SetIdsForOutgoingMessage(ctx));

        await harness.Consumed.Any<SomeOutgoingMessage>();

        receiveObserver.OutgoingConversationId.Should().Be(receiveObserver.IncomingConversationId);
        receiveObserver.OutgoingInitiatorId.Should().Be(correlationId);
        receiveObserver.OutgoingCorrelationId.Should().BeNull();
    }

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
}
