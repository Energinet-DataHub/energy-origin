using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using FluentAssertions;
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
    public async Task RequestReportGeneration_ShouldReturnValidReportId()
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Headers.Location.Should().NotBeNull("we expect a Location header with the new ReportId");

        var location = response.Headers.Location!.ToString();
        location.Should().Contain("ReportId=", "the controller appends it as a query parameter");

        var raw = location[(location.IndexOf("ReportId=", StringComparison.OrdinalIgnoreCase) + "ReportId=".Length)..];
        var amp = raw.IndexOf('&');
        var idString = amp >= 0 ? raw[..amp] : raw;

        var reportId = Guid.Parse(idString);
        reportId.Should().NotBe(Guid.Empty, "the controller must generate a real ReportId");
    }
}
