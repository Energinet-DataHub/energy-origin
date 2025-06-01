using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlPdfGenerator.API.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlPdfGenerator.IntegrationTests;

public class PdfGenerationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Given_CallToGeneratePdfEndpoint_When_ReceivingValidHtml_Then_RenderPdf_And_ReturnByteArray()
    {
        // Arrange
        var input = new HtmlInput { Html = "<html><body><h1>Hello, PDF!</h1></body></html>" };
        var content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/generate-pdf", content, TestContext.Current.CancellationToken);
        var pdfBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.True(pdfBytes.Length > 100, "PDF file should not be empty");
    }
}
