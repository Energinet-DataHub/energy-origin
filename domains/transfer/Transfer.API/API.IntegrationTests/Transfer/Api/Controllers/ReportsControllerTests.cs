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
using FluentAssertions;
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
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var endDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestBody = new { StartDate = startDate, EndDate = endDate };
        var client = _factory.CreateB2CAuthenticatedClient(_sub, _orgId.Value);

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", requestBody, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert Http response
        response.Headers.Location.Should().NotBeNull();
        var segments = response.Headers.Location!.Segments;
        var reportId = Guid.Parse(segments.Last().TrimEnd('/'));

        // Assert DB record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var report = db.Reports.Single(x => x.Id == reportId);
        report.Status.Should().Be(ReportStatus.Pending);
        report.StartDate.EpochSeconds.Should().Be(startDate);
        report.EndDate.EpochSeconds.Should().Be(endDate);
    }
}
