using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using API.Transfer.Api.Controllers;
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
    public async Task RequestReportGeneration_ShouldReturnValidReportResponse()
    {
        // Arrange
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(sub, orgId);

        var startDate = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var endDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestBody = new { StartDate = startDate, EndDate = endDate };

        // Act
        var response = await client.PostAsJsonAsync($"/api/reports?organizationId={orgId}", requestBody, cancellationToken: TestContext.Current.CancellationToken);
        var bodyJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull();
        var body = JsonSerializer.Deserialize<ReportGenerationResponse>(bodyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        body.Should().NotBeNull();
        body!.ReportId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task DownloadReport_ShouldReturnFile_WhenReportIsCompleted()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(sub, orgId);

        var content = new byte[] { 1, 2, 3, 4, 5 };
        var report = Report.Create(reportId, OrganizationId.Create(orgId), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), UnixTimestamp.Now().AddDays(-14), UnixTimestamp.Now().AddDays(-7));
        report.MarkCompleted(content);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext.ApplicationDbContext>();
            dbContext.Reports.Add(report);
            dbContext.SaveChanges();
        }

        var response = await client.GetAsync($"/api/reports/{reportId}/download?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        responseContent.Should().BeEquivalentTo(content);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DownloadReport_ShouldReturnNotFound_WhenReportIsNotCompleted()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var client = _factory.CreateB2CAuthenticatedClient(sub, orgId);

        var report = Report.Create(reportId, OrganizationId.Create(orgId), OrganizationName.Create("Organization Name"), Tin.Create("13371337"), UnixTimestamp.Now().AddDays(-14), UnixTimestamp.Now().AddDays(-7));
        report.MarkFailed();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext.ApplicationDbContext>();
            dbContext.Reports.Add(report);
            dbContext.SaveChanges();
        }

        var response = await client.GetAsync($"/api/reports/{reportId}/download?organizationId={orgId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
