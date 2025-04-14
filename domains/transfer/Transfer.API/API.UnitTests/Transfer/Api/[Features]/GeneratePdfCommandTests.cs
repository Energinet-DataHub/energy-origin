using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Setup.Pdf;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class GeneratePdfCommandTests
{
    [Fact]
    public async Task Handle_ReturnsPdfResult_WhenResponseIsSuccessful()
    {
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html><body><h1>Hello, PDF!</h1></body></html>"));
        var command = new GeneratePdfCommand(base64Html);
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%FakeContent\n...");
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .When(HttpMethod.Post, "http://example.com")
            .Respond(req =>
            {
                var message = new HttpResponseMessage(HttpStatusCode.OK);
                message.Content = new ByteArrayContent(pdfBytes);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                return message;
            });

        var httpClient = new HttpClient(mockHttpMessageHandler);
        var pdfOptions = Options.Create(new PdfOptions { Url = "http://example.com" });
        var handler = new GeneratePdfCommandHandler(httpClient, pdfOptions);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PdfBytes);
        Assert.Equal(pdfBytes, result.PdfBytes);
    }

    [Fact]
    public async Task Handle_ReturnsErrorResult_WhenResponseIsUnsuccessful()
    {
        // Arrange
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html><body><h1>Hello, PDF!</h1></body></html>"));
        var command = new GeneratePdfCommand(base64Html);
        var errorMessage = "Error generating PDF";
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .When(HttpMethod.Post, "http://example.com")
            .Respond(req =>
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest);
                message.Content = new StringContent(errorMessage);
                return message;
            });
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var pdfOptions = Options.Create(new PdfOptions { Url = "http://example.com" });
        var handler = new GeneratePdfCommandHandler(httpClient, pdfOptions);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.PdfBytes);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(errorMessage, result.ErrorContent);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenBase64HtmlIsInvalid()
    {
        // Arrange
        var invalidBase64Html = "InvalidBase64";
        var command = new GeneratePdfCommand(invalidBase64Html);
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler);
        var pdfOptions = Options.Create(new PdfOptions { Url = "http://example.com" });
        var handler = new GeneratePdfCommandHandler(httpClient, pdfOptions);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => handler.Handle(command, CancellationToken.None));
    }
}
