using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Fixtures;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class PdfControllerTests
{
    private readonly IntegrationTestFixture _fixture;

    public PdfControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        _fixture = integrationTestFixture;
        var pdfGeneratorWireMock = integrationTestFixture.PdfGeneratorWireMock;
        pdfGeneratorWireMock.ResetMappings();
    }

    [Fact]
    public async Task ReturnsPdf()
    {
        await using var test = _fixture.CreateIsolatedWireMockTest();

        test.StubRequest(
            req => req.WithPath("/generate-pdf").UsingPost(),
            res => res
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/pdf")
                .WithBody(Encoding.ASCII.GetBytes("%PDF-stub")));

        var html = "<html><body><h1>Hello!</h1></body></html>";
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
        var requestBody = new { Base64Html = base64Html };

        var url = $"api/transfer/pdf/generate?organizationId={test.OrganizationId.Value}";

        var response = await test.Client.PostAsJsonAsync(url, requestBody,
            cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(body));
    }

    [Fact]
    public async Task ReturnsErrorFromPdfService()
    {
        await using var test = _fixture.CreateIsolatedWireMockTest();

        test.StubRequest(
            req => req.WithPath("/generate-pdf").UsingPost(),
            res => res.WithStatusCode(500));

        var html = "<html><body><h1>Error</h1></body></html>";
        var requestBody = new { Base64Html = html }; // Client forgets to base64 Encode

        var url = $"api/transfer/pdf/generate?organizationId={test.OrganizationId.Value}";

        var response = await test.Client.PostAsJsonAsync(url, requestBody,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
