using DataContext.Models;
using DataContext;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using EnergyOrigin.ActivityLog.API;
using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Transfer.TransferAgreementCleanup;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementCleanupTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly Guid sub;
    private readonly string tin;

    public TransferAgreementCleanupTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
        sub = Guid.NewGuid();
        tin = "11223344";
    }

    [Fact]
    public async Task ShouldOnlyDeleteExpiredTransferAgreements()
    {
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync();

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            ReceiverTin = "12345678",
            SenderName = "SomeSender",
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            TransferAgreementNumber = 0
        };
        var nullEndDateTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = null,
            ReceiverTin = "12345679",
            SenderName = "SomeSender",
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            TransferAgreementNumber = 1
        };
        var endDateTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = DateTimeOffset.UtcNow.AddHours(1),
            ReceiverTin = "12345677",
            SenderName = "SomeSender",
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            TransferAgreementNumber = 2
        };

        dbContext.TransferAgreements.Add(expiredTa);
        dbContext.TransferAgreements.Add(nullEndDateTa);
        dbContext.TransferAgreements.Add(endDateTa);
        await dbContext.SaveChangesAsync();

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(2);

        tas.Count.Should().Be(2);
        tas.Select(x => x.Id).Should().Contain(nullEndDateTa.Id);
        tas.Select(x => x.Id).Should().Contain(endDateTa.Id);
        tas.Select(x => x.Id).Should().NotContain(expiredTa.Id);
    }

    [Fact]
    public async Task ShouldProduceActivityLogEntriesForReceiverAndSender()
    {

        var receiverTin = "12345677";
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync();

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            ReceiverTin = receiverTin,
            SenderName = "SomeSender",
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            TransferAgreementNumber = 0
        };

        dbContext.TransferAgreements.Add(expiredTa);
        await dbContext.SaveChangesAsync();

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(0, TimeSpan.FromSeconds(30));
        tas.Should().BeEmpty();

        var senderClient = factory.CreateAuthenticatedClient(sub.ToString(), tin: tin);

        var senderPost = await senderClient.PostAsJsonAsync("api/transfer/activity-log",
            new ActivityLogEntryFilterRequest(null, null, null));
        senderPost.StatusCode.Should().Be(HttpStatusCode.OK);
        var senderLogResponseBody = await senderPost.Content.ReadAsStringAsync();
        var senderLogs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(senderLogResponseBody);
        senderLogs!.ActivityLogEntries.Should().ContainSingle();

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);

        var receiverPost = await receiverClient.PostAsJsonAsync("api/transfer/activity-log",
            new ActivityLogEntryFilterRequest(null, null, null));
        receiverPost.StatusCode.Should().Be(HttpStatusCode.OK);
        var receiverLogResponseBody = await receiverPost.Content.ReadAsStringAsync();
        var receiverLogs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(receiverLogResponseBody);
        receiverLogs!.ActivityLogEntries.Should().ContainSingle();

        dbContext.TransferAgreements.RemoveRange(dbContext.TransferAgreements);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ShouldDeleteTaHistoryEntries()
    {
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync();

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            ReceiverTin = "12345678",
            SenderName = "SomeSender",
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            TransferAgreementNumber = 0
        };


        dbContext.TransferAgreements.Add(expiredTa);
        await dbContext.SaveChangesAsync();

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(0, TimeSpan.FromSeconds(30));
        tas.Should().BeEmpty();
    }
}
