using System;
using System.Threading;
using System.Threading.Tasks;
using API.Events;
using API.Transfer.Api._Features_;
using EnergyOrigin.IntegrationEvents.Events.OrganizationPromotedToProduction.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Events;
public class TransferOrganizationPromotedToProductionEventHandlerTests
{
    [Fact]
    public async Task Consume()
    {
        var mediatrMock = Substitute.For<IMediator>();
        var sut = new TransferOrganizationPromotedToProductionEventHandler(mediatrMock, Substitute.For<ILogger<TransferOrganizationPromotedToProductionEventHandler>>());

        var mockContext = Substitute.For<ConsumeContext<OrganizationPromotedToNormal>>();
        var message = OrganizationPromotedToNormal.Create(Guid.NewGuid(), Any.Tin().ToString());
        mockContext.Message.Returns(message);

        await sut.Consume(mockContext);

        await mediatrMock.Received(1).Send(Arg.Any<DeleteTransferAgreementsCommand>(), Arg.Any<CancellationToken>());
        await mediatrMock.Received(1).Send(Arg.Any<DeleteClaimAutomationArgsCommand>(), Arg.Any<CancellationToken>());
    }
}
