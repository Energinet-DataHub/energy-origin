using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.IntegrationTests.Setup.Fixtures;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Domain.ValueObjects.Tests;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class PdfControllerTests
{
    private readonly Guid sub = Guid.NewGuid();
    private readonly OrganizationId orgId = Any.OrganizationId();
    private readonly Tin tin = Tin.Create("12345678");
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public PdfControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task CreatePdf_ReturnsPdfFile()
    {
        // Arrange
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId.Value, tin.Value);
        var html = "<html><body><h1>Hello, PDF!</h1></body></html>";
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));

        var request = new
        {
            Base64Html = base64Html
        };

        var url = $"api/transfer/pdf/generate?organizationId={orgId.Value}";

        // Act
        var response = await client.PostAsJsonAsync(url, request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        var pdfHeader = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 4));
        Assert.Equal("%PDF", pdfHeader);
    }
}
