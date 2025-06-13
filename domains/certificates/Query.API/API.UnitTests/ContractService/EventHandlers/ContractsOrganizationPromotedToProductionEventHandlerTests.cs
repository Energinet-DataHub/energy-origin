using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;
using MassTransit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using API.ContractService.EventHandlers;
using API.ContractService.Internal;

namespace API.UnitTests.ContractService.EventHandlers;

public class ContractsOrganizationPromotedToProductionEventHandlerTests
{
    [Fact]
    public async Task CallsRemoveOrganizationContractsAndSlidingWindowsCommand()
    {
        var mediatrMock = Substitute.For<IMediator>();
        var sut = new ContractsOrganizationPromotedToProductionEventHandler(mediatrMock, Substitute.For<ILogger<ContractsOrganizationPromotedToProductionEventHandler>>());

        var mockContext = Substitute.For<ConsumeContext<OrganizationPromotedToNormal>>();
        var message = OrganizationPromotedToNormal.Create(Guid.NewGuid());
        mockContext.Message.Returns(message);

        await sut.Consume(mockContext);

        await mediatrMock.Received(1).Send(Arg.Any<RemoveOrganizationContractsAndSlidingWindowsCommand>(), Arg.Any<CancellationToken>());
    }
}
