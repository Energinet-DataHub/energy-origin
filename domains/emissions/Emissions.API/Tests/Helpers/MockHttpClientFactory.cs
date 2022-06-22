using System;
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

    public static HttpClient SetupHttpClientFromFile(string resourceName)
    {
        var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid directory");
        var path = System.IO.Path.Combine(directory, "../../../Resources/", resourceName);
        string json = File.ReadAllText(path);
        return SetupHttpClient(json);
    }

    public static HttpClient SetupHttpClient(string serialize)
    {

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            // Setup the PROTECTED method to mock
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            // prepare the expected response of the mocked http call
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(serialize),
            }).Verifiable();


        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://example.com/"),
        };
    }
}
