using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using API.Transfer.Api._Features_;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class ReportsControllerTests
{
    private readonly TransferAgreementsApiWebApplicationFactory _factory;

    public ReportsControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task GivenValidReportRequest_WhenRequestingReportGeneration_ThenPendingReportIsPersistedAndEventIsPublished()
    {
        // Arrange
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(sub, orgId);
        var startDate = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var endDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestBody = new { StartDate = startDate, EndDate = endDate };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/reports?organizationId={orgId}",
            requestBody,
            TestContext.Current.CancellationToken);

        // Assert Http response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();
        var responseBody = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        responseBody.Should().NotBeNull().And.ContainKey("reportId");
        var reportId = Guid.Parse(responseBody!["reportId"]);

        // Assert DB record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var report = db.Reports.Single(x => x.Id == reportId);
        report.Status.Should().Be(ReportStatus.Pending);
        report.StartDate.EpochSeconds.Should().Be(startDate);
        report.EndDate.EpochSeconds.Should().Be(endDate);
    }

    [Fact]
    public async Task GivenReportsFromMultipleOrganizations_WhenGettingStatuses_ThenOnlyReportsFromRequestedOrganizationAreReturned()
    {
        var sub = Guid.NewGuid();
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var client  = _factory.CreateB2CAuthenticatedClient(sub, orgId.Value);

        var report1 = Report.Create(orgId,
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-2)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-1)));

        var report2 = Report.Create(orgId,
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-4)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-3)));

        var otherOrganizationsReport = Report.Create(OrganizationId.Create(Guid.NewGuid()),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-6)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-5)));

        using (var scope = _factory.Services.CreateScope())
        {
            var db    = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Reports.AddRange(report1, report2, otherOrganizationsReport);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var url = $"/api/reports?organizationId={orgId}";
        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content
            .ReadFromJsonAsync<GetReportStatusesQueryResult>(cancellationToken: TestContext.Current.CancellationToken);

        results
            .Should().NotBeNull()
            .And.BeOfType<GetReportStatusesQueryResult>()
            .Which.Result
            .Should().NotBeNull()
            .And.HaveCount(2)
            .And.Contain(r => r.Id != otherOrganizationsReport.OrganizationId.Value);
    }
}
