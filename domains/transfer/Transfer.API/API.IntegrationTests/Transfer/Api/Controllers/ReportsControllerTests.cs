using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Domain.ValueObjects.Tests;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class ReportsControllerTests
{
    private readonly Guid _sub = Guid.NewGuid();
    private readonly OrganizationId _orgId = Any.OrganizationId();

    private readonly TransferAgreementsApiWebApplicationFactory _factory;

    public ReportsControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task RequestReportGeneration_ShouldPersistPendingReport_AndPublishEvent()
    {
        var startDate = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var endDate   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestBody = new { StartDate = startDate, EndDate = endDate };
        var httpClient  = _factory.CreateB2CAuthenticatedClient(_sub, _orgId.Value);

        var response = await httpClient.PostAsJsonAsync("/api/reports", requestBody, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var reportId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: TestContext.Current.CancellationToken);
        reportId.Should().NotBe(Guid.Empty);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var report = db.Reports.Single(x => x.Id == reportId);
            report.Should().NotBeNull()
                .And.BeOfType<Report>()
                .Which.Status.Should().Be(ReportStatus.Pending);
            report.Id.Should().Be(reportId);
            report.StartDate.EpochSeconds.Should().Be(startDate);
            report.EndDate.EpochSeconds.Should().Be(endDate);
        }

        var harness = _factory.Services
            .GetRequiredService<ITestHarness>();
        Assert.True(await harness.Published.Any<ReportRequestCreated>(TestContext.Current.CancellationToken));

        var published = harness.Published
            .Select<ReportRequestCreated>(TestContext.Current.CancellationToken).First().Context.Message;

        published.ReportId.Should().Be(reportId);
        published.StartDate.Should().Be(startDate);
        published.EndDate.Should().Be(endDate);
    }
}
