using API.Authorization._Features_.Events;
using API.Authorization._Features_.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationWhitelisted;
using MassTransit.Testing;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace API.UnitTests._Features_.Events;

public class OrganizationWhitelistedIntegrationEventHandlerTests
{
    [Fact]
    public async Task Given_Message_When_HandlerSucceeds_Then_MessageShouldBeConsumed()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<WhitelistOrganizationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new OrganizationWhitelistedIntegrationEventHandler(mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new OrganizationWhitelistedIntegrationEvent(
                id: Guid.NewGuid(),
                traceId: "trace-1",
                created: DateTimeOffset.UtcNow,
                tin: Any.Tin().Value
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            var consumed = await consumerHarness.Consumed
                .SelectAsync<OrganizationWhitelistedIntegrationEvent>(TestContext.Current.CancellationToken)
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

        mediator.Send(Arg.Any<WhitelistOrganizationCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB Failure"));

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new OrganizationWhitelistedIntegrationEventHandler(mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new OrganizationWhitelistedIntegrationEvent(
                id: Guid.NewGuid(),
                traceId: "trace-1",
                created: DateTimeOffset.UtcNow,
                tin: Any.Tin().Value
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            var consumed = await consumerHarness.Consumed
                .SelectAsync<OrganizationWhitelistedIntegrationEvent>(TestContext.Current.CancellationToken).FirstOrDefault();

            Assert.NotNull(consumed);
            Assert.False(consumed.Context.ReceiveContext.IsDelivered);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
