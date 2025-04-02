using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.EventHandlers;
using API.ContractService.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace API.IntegrationTests.ContractService.EventHandlers;

public class ContractsOrganizationRemovedFromWhitelistEventHandlerTests
{
    [Fact]
    public async Task Given_Message_When_HandlerSucceeds_Then_MessageShouldBeConsumed()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<ContractsOrganizationRemovedFromWhitelistEventHandler>>();

        mediator.Send(Arg.Any<RemoveOrganizationContractsAndSlidingWindowsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RemoveOrganizationContractsAndSlidingWindowsCommandResult()));

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new ContractsOrganizationRemovedFromWhitelistEventHandler(logger, mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new OrganizationRemovedFromWhitelist(
                id: Guid.NewGuid(),
                traceId: "trace-abc",
                created: DateTimeOffset.UtcNow,
                organizationId: Guid.NewGuid(),
                tin: "11223344"
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            var consumed = await consumerHarness.Consumed
                .SelectAsync<OrganizationRemovedFromWhitelist>(TestContext.Current.CancellationToken).FirstOrDefault();

            Assert.NotNull(consumed);
            Assert.True(await consumerHarness.Consumed.Any<OrganizationRemovedFromWhitelist>(TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Given_Message_When_HandlerFails_Then_MessageShouldBeMovedToErrorQueue()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<ContractsOrganizationRemovedFromWhitelistEventHandler>>();

        mediator.Send(Arg.Any<RemoveOrganizationContractsAndSlidingWindowsCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Oops"));

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new ContractsOrganizationRemovedFromWhitelistEventHandler(logger, mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new OrganizationRemovedFromWhitelist(
                id: Guid.NewGuid(),
                traceId: "trace-fail",
                created: DateTimeOffset.UtcNow,
                organizationId: Guid.NewGuid(),
                tin: "11223344"
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            Assert.True(await consumerHarness.Consumed.Any<OrganizationRemovedFromWhitelist>(TestContext.Current.CancellationToken));
            Assert.True(await harness.Published.Any<Fault<OrganizationRemovedFromWhitelist>>(TestContext.Current.CancellationToken));

            var fault = await harness.Published.SelectAsync<Fault<OrganizationRemovedFromWhitelist>>(TestContext.Current.CancellationToken).FirstOrDefault();
            Assert.NotNull(fault);
            Assert.Equal(evt.OrganizationId, fault.Context.Message.Message.OrganizationId);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
