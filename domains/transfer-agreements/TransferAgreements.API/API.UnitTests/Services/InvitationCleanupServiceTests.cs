using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Services.InvitationCleanup;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Services;

public class InvitationCleanupServiceTests
{

    [Fact]
    public async Task Run_ShouldCallDeleteOldInvitations_WhenInvoked()
    {
        var logger = Substitute.For<ILogger<InvitationCleanupService>>();
        var invitationRepository = Substitute.For<IInvitationRepository>();
        var invitationCleanupService = new InvitationCleanupService(logger, invitationRepository);

        var cancellationTokenSource = new CancellationTokenSource();
        var stoppingToken = cancellationTokenSource.Token;

        var runTask = invitationCleanupService.Run(stoppingToken);

        await Task.Delay(100, stoppingToken);
        cancellationTokenSource.Cancel();

        await runTask;

        await invitationRepository.Received(1).DeleteOldInvitations(Arg.Is<TimeSpan>(ts => ts == TimeSpan.FromDays(14)));
        invitationRepository.ReceivedCalls().Should().HaveCount(1, "because the DeleteOldInvitations method should be called once.");
    }

    [Fact]
    public async Task Run_ShouldDeleteOnlyOldInvitations_WhenInvoked()
    {
        var logger = Substitute.For<ILogger<InvitationCleanupService>>();
        var invitationRepository = Substitute.For<IInvitationRepository>();
        var invitationCleanupService = new InvitationCleanupService(logger, invitationRepository);

        var oldInvitation = new Invitation { CreatedAt = DateTimeOffset.Now.AddDays(-15) };
        var newInvitation = new Invitation { CreatedAt = DateTimeOffset.Now };

        var deletedInvitations = new List<Invitation>();

        await invitationRepository.DeleteOldInvitations(Arg.Do<TimeSpan>(ts =>
        {
            if (oldInvitation.CreatedAt < DateTimeOffset.Now - ts)
            {
                deletedInvitations.Add(oldInvitation);
            }
        }));

        var cancellationTokenSource = new CancellationTokenSource();
        var stoppingToken = cancellationTokenSource.Token;

        var runTask = invitationCleanupService.Run(stoppingToken);

        await Task.Delay(100, stoppingToken);
        cancellationTokenSource.Cancel();

        await runTask;

        deletedInvitations.Should().Contain(oldInvitation, "because old invitations should be deleted.");
        deletedInvitations.Should().NotContain(newInvitation, "because new invitations should not be deleted.");
    }
}
