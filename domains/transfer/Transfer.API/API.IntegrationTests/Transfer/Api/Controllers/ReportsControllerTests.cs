using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using DataContext;
using DataContext.Models;
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
    public async Task RequestReportGeneration_ShouldPersistPendingReport()
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

        // Assert HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();

        var location = response.Headers.Location!.ToString();
        location.Should().Contain("ReportId=", because: "we return it as a query parameter");

        var raw = location.Substring(
            location.IndexOf("ReportId=", StringComparison.OrdinalIgnoreCase)
            + "ReportId=".Length);
        var amp = raw.IndexOf('&');
        var idString = amp >= 0 ? raw.Substring(0, amp) : raw;
        var reportId = Guid.Parse(idString);

        // Assert DB record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var report = db.Reports.Single(x => x.Id == reportId);
        report.Status.Should().Be(ReportStatus.Pending);
        report.StartDate.EpochSeconds.Should().Be(startDate);
        report.EndDate.EpochSeconds.Should().Be(endDate);
    }
}
