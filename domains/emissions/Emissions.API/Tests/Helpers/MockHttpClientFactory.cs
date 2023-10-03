using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
namespace Tests.Helpers;

public static class MockHttpClientFactory
{
    public static HttpClient SetupHttpClientWithFiles(List<string> resources)
    {
        var handlerMock = Substitute.For<HttpMessageHandler>();
        var handlerPart = handlerMock.GetType().GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(handlerMock, new object[] { Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>() });

        var response = new Queue<object>();
        foreach (var item in resources)
        {
            var content = File.ReadAllText($"Resources/{item}");
            var contentSequence = new StringContent(content);

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = contentSequence
            };
            response.Enqueue(Task.FromResult(httpResponseMessage));
        }
        handlerPart.Returns(response.Dequeue(), response.ToArray());

        return new HttpClient(handlerMock);
    }
}

