using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace Tests.Helpers;

public static class MockHttpClientFactory
{

    public static string ReadJsonFiles(string resourceName)
    {
        var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid directory");
        var path = System.IO.Path.Combine(directory, "../../../Resources/", resourceName);
        var json = File.ReadAllText(path);
        return json;
    }

    public static HttpClient SetupHttpClientWithFiles(List<string> resourceName)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var handlerPart = handlerMock
            .Protected()
            // Setup the PROTECTED method to mock
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );

        foreach (var resourceNameItem in resourceName)
        {
            var content = ReadJsonFiles(resourceNameItem);
            var contentSequence = new StringContent(content);
            handlerPart = handlerPart.ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = contentSequence
            });
        }
        handlerMock.Verify();

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://example.com/"),
        };
    }
}

