using API.Authorization._Features_.Internal;
using API.Authorization.EventHandlers;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace API.UnitTests.Eventhandlers;

public class AuthorizationOrganizationRemovedFromWhitelistEventHandlerTests
{
    [Fact]
    public async Task Given_Message_When_HandlerSucceeds_Then_AllThreeCommandsAreCalled_And_MessageIsConsumed()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<AuthorizationOrganizationRemovedFromWhitelistEventHandler>>();

        var removeConsentsResult = new RemoveOrganizationConsentsCommandResult();
        var removeClientsResult = new RemoveOrganizationClientsCommandResult();
        var deactivateOrganizationCommandResult = new DeactivateOrganizationCommandResult();

        mediator.Send(Arg.Any<RemoveOrganizationConsentsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(removeConsentsResult));
        mediator.Send(Arg.Any<RemoveOrganizationClientsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(removeClientsResult));
        mediator.Send(Arg.Any<DeactivateOrganizationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deactivateOrganizationCommandResult));

        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new AuthorizationOrganizationRemovedFromWhitelistEventHandler(logger, mediator));

        await harness.Start(TestContext.Current.CancellationToken);
        try
        {
            var evt = new OrganizationRemovedFromWhitelist(
                id: Guid.NewGuid(),
                traceId: "trace-123",
                created: DateTimeOffset.UtcNow,
                organizationId: Guid.NewGuid(),
                tin: "55667788"
            );

            await harness.InputQueueSendEndpoint.Send(evt, TestContext.Current.CancellationToken);

            Assert.True(
                await consumerHarness.Consumed.Any<OrganizationRemovedFromWhitelist>(TestContext.Current
                    .CancellationToken));

            await mediator.Received(1)
                .Send(Arg.Is((RemoveOrganizationConsentsCommand cmd) => cmd.OrganizationId.Value == evt.OrganizationId),
                    Arg.Any<CancellationToken>());

            await mediator.Received(1)
                .Send(Arg.Is((RemoveOrganizationClientsCommand cmd) => cmd.OrganizationId.Value == evt.OrganizationId),
                    Arg.Any<CancellationToken>());

            await mediator.Received(1)
                .Send(Arg.Is((DeactivateOrganizationCommand cmd) => cmd.OrganizationId == evt.OrganizationId),
                    Arg.Any<CancellationToken>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
