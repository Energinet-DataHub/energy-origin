using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

public class MessageWrapperTests : IAsyncDisposable
{
    private readonly ReceiveObserver observer;
    private readonly ServiceProvider provider;
    private readonly ITestHarness harness;

    public MessageWrapperTests()
    {
        observer = new ReceiveObserver();
        provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddReceiveObserver(_ => observer);
                cfg.AddConsumer<SomeMessageConsumer>();
            })
            .BuildServiceProvider(true);

        harness = provider.GetRequiredService<ITestHarness>();
    }

    [Fact]
    public async Task SetIdsForOutgoingMessage_IdsAreSetForAMessageWithoutCorrelationId()
    {
        await harness.Start();

        await harness.Bus.Publish(new SomeIncomingMessage(Guid.NewGuid(), "foo"));

        await harness.Consumed.Any<SomeIncomingMessage>();

        var wrapper = observer.WrappedIncomingMessage!;

        await harness.Bus.Publish(
            new SomeOutgoingMessage(wrapper.Message.SomeId, 42),
            ctx => wrapper.SetIdsForOutgoingMessage(ctx));

        await harness.Consumed.Any<SomeOutgoingMessage>();

        observer.OutgoingConversationId.Should().Be(observer.IncomingConversationId);
        observer.OutgoingInitiatorId.Should().BeNull();
        observer.OutgoingCorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task SetIdsForOutgoingMessage_IdsAreSetForAMessageWithCorrelationId()
    {
        await harness.Start();

        var correlationId = Guid.NewGuid();

        await harness.Bus.Publish(
            new SomeIncomingMessage(Guid.NewGuid(), "foo"),
            ctx => ctx.CorrelationId = correlationId);

        await harness.Consumed.Any<SomeIncomingMessage>();

        var wrapper = observer.WrappedIncomingMessage!;

        await harness.Bus.Publish(
            new SomeOutgoingMessage(wrapper.Message.SomeId, 42),
            ctx => wrapper.SetIdsForOutgoingMessage(ctx));

        await harness.Consumed.Any<SomeOutgoingMessage>();

        observer.OutgoingConversationId.Should().Be(observer.IncomingConversationId);
        observer.OutgoingInitiatorId.Should().Be(correlationId);
        observer.OutgoingCorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task ConversationIdNotSetWhenNotUsingWrapper()
    {
        await harness.Start();

        var someId = Guid.NewGuid();
        await harness.Bus.Publish(new SomeIncomingMessage(someId, "foo"));

        await harness.Consumed.Any<SomeIncomingMessage>();

        await harness.Bus.Publish(new SomeOutgoingMessage(someId, 42));

        await harness.Consumed.Any<SomeOutgoingMessage>();

        observer.OutgoingConversationId!.Value.Should().NotBe(observer.IncomingConversationId!.Value);
        observer.OutgoingInitiatorId.Should().BeNull();
        observer.OutgoingCorrelationId.Should().BeNull();
    }

    private record SomeIncomingMessage(Guid SomeId, string SomeValue);

    private record SomeOutgoingMessage(Guid SomeId, int SomeValue);

    private class SomeMessageConsumer : IConsumer<SomeIncomingMessage>, IConsumer<SomeOutgoingMessage>
    {
        public Task Consume(ConsumeContext<SomeIncomingMessage> context) => Task.CompletedTask;

        public Task Consume(ConsumeContext<SomeOutgoingMessage> context) => Task.CompletedTask;
    }

    private class ReceiveObserver : IReceiveObserver
    {
        public MessageWrapper<SomeIncomingMessage>? WrappedIncomingMessage { get; private set; }
        public Guid? IncomingConversationId { get; private set; }
        public Guid? IncomingCorrelationId { get; private set; }
        public Guid? IncomingInitiatorId { get; private set; }

        public Guid? OutgoingConversationId { get; private set; }
        public Guid? OutgoingCorrelationId { get; private set; }
        public Guid? OutgoingInitiatorId { get; private set; }

        public Task PreReceive(ReceiveContext context)
            => Task.CompletedTask;

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

        public Task PostReceive(ReceiveContext context)
            => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
            => Task.CompletedTask;

        public Task ReceiveFault(ReceiveContext context, Exception exception)
            => Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}
