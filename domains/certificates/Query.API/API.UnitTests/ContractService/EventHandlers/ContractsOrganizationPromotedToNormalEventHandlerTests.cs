using MassTransit;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using API.ContractService.EventHandlers;
using API.ContractService.Internal;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToNormal.V1;

namespace API.UnitTests.ContractService.EventHandlers;

public class ContractsOrganizationPromotedToNormalEventHandlerTests
{
    [Fact]
    public async Task CallsRemoveOrganizationContractsAndSlidingWindowsCommand()
    {
        var mediatrMock = Substitute.For<IMediator>();
        var sut = new ContractsOrganizationPromotedToNormalEventHandler(mediatrMock, Substitute.For<ILogger<ContractsOrganizationPromotedToNormalEventHandler>>());

        var mockContext = Substitute.For<ConsumeContext<OrganizationPromotedToNormal>>();
        var message = OrganizationPromotedToNormal.Create(Guid.NewGuid());
        mockContext.Message.Returns(message);

        await sut.Consume(mockContext);

        await mediatrMock.Received(1).Send(Arg.Any<RemoveOrganizationContractsAndSlidingWindowsCommand>(), Arg.Any<CancellationToken>());
    }
}
