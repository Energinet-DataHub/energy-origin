using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;

namespace Tests.Helpers;

public static class MockHttpClientFactory
{

    public static HttpClient SetupHttpClientFromFile(string resourceName)
    {
        var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid directory");
        var path = Path.Combine(directory, "../../../Resources/", resourceName);
        var json = File.ReadAllText(path);
        return SetupHttpClient(json);
    }

    public static HttpClient SetupHttpClient(string serialize)
    {
        var handlerMock = Substitute.For<HttpMessageHandler>();

        var httpResponseMessage = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(serialize),
        };
        var task = Task.FromResult(httpResponseMessage);

        handlerMock.GetType().GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(handlerMock, new object[] { Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>() })
            .Returns(task);

        return new HttpClient(handlerMock)
        {
            BaseAddress = new Uri("http://example.com/"),
        };
    }
}
