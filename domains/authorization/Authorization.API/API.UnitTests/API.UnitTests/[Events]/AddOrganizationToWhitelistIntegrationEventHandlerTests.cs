using API.Authorization._Events_;
using API.Authorization._Features_.Internal;
using EnergyOrigin.IntegrationEvents.Events.AddOrganizationToWhitelist;
using MassTransit.Testing;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace API.UnitTests._Events_;

public class AddOrganizationToWhitelistIntegrationEventHandlerTests
{
    [Fact]
    public async Task Given_Message_When_HandlerSucceeds_Then_MessageShouldBeConsumed()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<AddOrganizationToWhitelistCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new AddOrganizationToWhitelistIntegrationEventHandler(mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new AddOrganizationToWhitelistIntegrationEvent(
                id: Guid.NewGuid(),
                traceId: "trace-1",
                created: DateTimeOffset.UtcNow,
                tin: Any.Tin().Value
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            var consumed = await consumerHarness.Consumed
                .SelectAsync<AddOrganizationToWhitelistIntegrationEvent>(TestContext.Current.CancellationToken)
                .FirstOrDefault();

            Assert.NotNull(consumed);
            Assert.True(consumed.Context.ReceiveContext.IsDelivered);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Given_Message_When_HandlerFails_Then_DoNotConsumeMessage()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<AddOrganizationToWhitelistCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB Failure"));

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new AddOrganizationToWhitelistIntegrationEventHandler(mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new AddOrganizationToWhitelistIntegrationEvent(
                id: Guid.NewGuid(),
                traceId: "trace-1",
                created: DateTimeOffset.UtcNow,
                tin: Any.Tin().Value
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            var consumed = await consumerHarness.Consumed
                .SelectAsync<AddOrganizationToWhitelistIntegrationEvent>(TestContext.Current.CancellationToken).FirstOrDefault();

            Assert.NotNull(consumed);
            Assert.False(consumed.Context.ReceiveContext.IsDelivered);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
