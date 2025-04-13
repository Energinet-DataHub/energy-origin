using DataContext.Models;
using DataContext;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using EnergyOrigin.ActivityLog.API;
using System.Net;
using System.Net.Http.Json;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Domain.ValueObjects.Tests;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Transfer.TransferAgreementCleanup;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementCleanupTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly OrganizationId sub;
    private readonly Tin tin;

    public TransferAgreementCleanupTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
        sub = Any.OrganizationId();
        tin = Tin.Create("11223344");
    }

    [Fact(Skip = "Skip until new cleanup strategy is implemented")]
    public async Task ShouldOnlyDeleteExpiredTransferAgreements()
    {
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = UnixTimestamp.Now().AddYears(-3),
            ReceiverTin = Tin.Create("12345678"),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = UnixTimestamp.Now().AddYears(-4),
            TransferAgreementNumber = 0
        };
        var nullEndDateTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = null,
            ReceiverTin = Tin.Create("12345679"),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = UnixTimestamp.Now().AddDays(-1),
            TransferAgreementNumber = 1
        };
        var endDateTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = UnixTimestamp.Now().AddHours(1),
            ReceiverTin = Tin.Create("12345677"),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = UnixTimestamp.Now().AddDays(-1),
            TransferAgreementNumber = 2
        };

        dbContext.TransferAgreements.Add(expiredTa);
        dbContext.TransferAgreements.Add(nullEndDateTa);
        dbContext.TransferAgreements.Add(endDateTa);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(2);

        tas.Count.Should().Be(2);
        tas.Select(x => x.Id).Should().Contain(nullEndDateTa.Id);
        tas.Select(x => x.Id).Should().Contain(endDateTa.Id);
        tas.Select(x => x.Id).Should().NotContain(expiredTa.Id);
    }

    [Fact(Skip = "Skip until new cleanup strategy is implemented")]
    public async Task ShouldProduceActivityLogEntriesForReceiverAndSender()
    {

        var receiverTin = "12345677";
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = UnixTimestamp.Now().AddHours(-1),
            ReceiverTin = Tin.Create(receiverTin),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = UnixTimestamp.Now().AddDays(-1),
            TransferAgreementNumber = 0
        };

        dbContext.TransferAgreements.Add(expiredTa);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(0, TimeSpan.FromSeconds(30));
        tas.Should().BeEmpty();

        var senderClient = factory.CreateAuthenticatedClient(sub.Value.ToString(), tin: tin.Value);

        var senderPost = await senderClient.PostAsJsonAsync("api/transfer/activity-log",
            new ActivityLogEntryFilterRequest(null, null, null), TestContext.Current.CancellationToken);
        senderPost.StatusCode.Should().Be(HttpStatusCode.OK);
        var senderLogResponseBody = await senderPost.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var senderLogs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(senderLogResponseBody);
        senderLogs!.ActivityLogEntries.Should().ContainSingle();

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);

        var receiverPost = await receiverClient.PostAsJsonAsync("api/transfer/activity-log",
            new ActivityLogEntryFilterRequest(null, null, null), TestContext.Current.CancellationToken);
        receiverPost.StatusCode.Should().Be(HttpStatusCode.OK);
        var receiverLogResponseBody = await receiverPost.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var receiverLogs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(receiverLogResponseBody);
        receiverLogs!.ActivityLogEntries.Should().ContainSingle();

        dbContext.TransferAgreements.RemoveRange(dbContext.TransferAgreements);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Skip until new cleanup strategy is implemented")]
    public async Task ShouldDeleteTaHistoryEntries()
    {
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TransferAgreements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var expiredTa = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            EndDate = UnixTimestamp.Now().AddHours(-1),
            ReceiverTin = Tin.Create("12345678"),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = tin,
            ReceiverReference = Guid.NewGuid(),
            SenderId = sub,
            StartDate = UnixTimestamp.Now().AddDays(-1),
            TransferAgreementNumber = 0
        };


        dbContext.TransferAgreements.Add(expiredTa);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tas = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreement>(0, TimeSpan.FromSeconds(30));
        tas.Should().BeEmpty();
    }
}
