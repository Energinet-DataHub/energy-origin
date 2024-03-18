using System.Threading.Tasks;
using Xunit;
using Transfer.Application.Commands;
using Transfer.Application.Repositories;
using NSubstitute;
using EnergyOrigin.ActivityLog.API;
using System;
using Transfer.Domain.Entities;
using FluentAssertions;
using Transfer.Application.Exceptions;

namespace Transfer.Application.Tests.Commands;

public class CreateTransferAgreementProposalCommandTests
{
    [Fact]
    public async Task ShouldCreateTransferAgreementProposal()
    {
        var taRepo = Substitute.For<ITransferAgreementRepository>();
        var taProposalRepo = Substitute.For<ITransferAgreementProposalRepository>();
        var alRepo = Substitute.For<IActivityLogEntryRepository>();
        var userContext = Substitute.For<IUserContext>();

        taRepo.HasDateOverlap(Arg.Any<TransferAgreementProposal>()).Returns(false);

        var handler = new CreateTransferAgreementProposalCommandHandler(taRepo, taProposalRepo, alRepo, userContext);
        var cmd = new CreateTransferAgreementProposalCommand(
            startDate: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            endDate: null,
            receiverTin: "12345678");

        var response = await handler.Handle(cmd, default);

        response.StartDate.Should().Be(cmd.StartDate);
        response.EndDate.Should().Be(cmd.EndDate);
        response.ReceiverCompanyTin.Should().Be(cmd.ReceiverTin);
    }

    [Fact]
    public async Task ShouldThrowExceptionWhenDateOverlap()
    {
        var taRepo = Substitute.For<ITransferAgreementRepository>();
        var taProposalRepo = Substitute.For<ITransferAgreementProposalRepository>();
        var alRepo = Substitute.For<IActivityLogEntryRepository>();
        var userContext = Substitute.For<IUserContext>();

        taRepo.HasDateOverlap(Arg.Any<TransferAgreementProposal>()).Returns(true);

        var handler = new CreateTransferAgreementProposalCommandHandler(taRepo, taProposalRepo, alRepo, userContext);
        var cmd = new CreateTransferAgreementProposalCommand(
                       startDate: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                  endDate: null,
                                  receiverTin: "12345678");

        var action = () => handler.Handle(cmd, default);

        await action.Should().ThrowAsync<TransferAgreementOverlapsException>();
    }
}
