using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Services.ConnectionInvitationCleanup;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Services;

public class ConnectionInvitationCleanupServiceTests
{

    [Fact]
    public async Task Run_ShouldCallDeleteOldConnectionInvitations_WhenInvoked()
    {
        var logger = Substitute.For<ILogger<ConnectionInvitationCleanupService>>();
        var connectionInvitationRepository = Substitute.For<IConnectionInvitationRepository>();
        var connectionInvitationCleanupService = new ConnectionInvitationCleanupService(logger, connectionInvitationRepository);

        var cancellationTokenSource = new CancellationTokenSource();
        var stoppingToken = cancellationTokenSource.Token;

        var runTask = connectionInvitationCleanupService.Run(stoppingToken);

        await Task.Delay(100, stoppingToken);
        cancellationTokenSource.Cancel();

        await runTask;

        await connectionInvitationRepository.Received(1).DeleteOldConnectionInvitations(Arg.Is<TimeSpan>(ts => ts == TimeSpan.FromDays(14)));
        connectionInvitationRepository.ReceivedCalls().Should().HaveCount(1, "because the DeleteOldConnectionInvitations method should be called once.");
    }

    [Fact]
    public async Task Run_ShouldDeleteOnlyOldConnectionInvitations_WhenInvoked()
    {
        var logger = Substitute.For<ILogger<ConnectionInvitationCleanupService>>();
        var connectionInvitationRepository = Substitute.For<IConnectionInvitationRepository>();
        var connectionInvitationCleanupService = new ConnectionInvitationCleanupService(logger, connectionInvitationRepository);

        var oldConnectionInvitation = new ConnectionInvitation { CreatedAt = DateTimeOffset.Now.AddDays(-15) };
        var newConnectionInvitation = new ConnectionInvitation { CreatedAt = DateTimeOffset.Now };

        var deletedConnectionInvitations = new List<ConnectionInvitation>();

        await connectionInvitationRepository.DeleteOldConnectionInvitations(Arg.Do<TimeSpan>(ts =>
        {
            if (oldConnectionInvitation.CreatedAt < DateTimeOffset.Now - ts)
            {
                deletedConnectionInvitations.Add(oldConnectionInvitation);
            }
        }));

        var cancellationTokenSource = new CancellationTokenSource();
        var stoppingToken = cancellationTokenSource.Token;

        var runTask = connectionInvitationCleanupService.Run(stoppingToken);

        await Task.Delay(100, stoppingToken);
        cancellationTokenSource.Cancel();

        await runTask;

        deletedConnectionInvitations.Should().Contain(oldConnectionInvitation, "because old connection-invitations should be deleted.");
        deletedConnectionInvitations.Should().NotContain(newConnectionInvitation, "because new connection-invitations should not be deleted.");
    }
}
