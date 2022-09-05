# Mocking HttpClient when testing

* Status: Accepted
* Deciders: @PeterAGY, @ckr123, @duizer, @CodeReaper, @C-Christiansen, @MartinSchmidt
* Date: 2022-09-01

---

## Context and Problem Statement

When testing a class that depends on `HttpClient` (or `IHttpClientFactory`) you have to mock that. Since `HttpClient` does not implement an interface and has many methods that is not easy. Furthermore, we want our tests to be easy to read.

---

## Considered Options

* Use a library that mocks `HttpClient`. [richardszalay/mockhttp](https://github.com/richardszalay/mockhttp) seems to be the most popular.
* Using Moq (or write our own implementation) to replace HttpMessageHandler within HttpClient

---

## Decision Outcome

We chose to use [richardszalay/mockhttp](https://github.com/richardszalay/mockhttp).

Please note that the two options are not mutally exclusive as we are already using Moq. If something is not possible to do using [richardszalay/mockhttp](https://github.com/richardszalay/mockhttp), we can fallback to using Moq.


## Rationale

Mocking `HttpClient` using Moq or by our own implementation requires some detailed knowledge about the workings of `HttpClient`. With a library this detailed knowledge is not needed.

We found that of the libraries mocking `HttpClient` [richardszalay/mockhttp](https://github.com/richardszalay/mockhttp) had highest number of stars on Github of the libraries. There has not been changes to [richardszalay/mockhttp](https://github.com/richardszalay/mockhttp) in several years, but in this case this is a good thing as `HttpClient` is an old part of .NET.

The tests becomes to easier read. An example of using Moq:

```C#
Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
handlerMock
    .Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.LocalPath == "/api/v1/session/logout"), ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
HttpClient client = new HttpClient(handlerMock.Object);

// Inject 'client' to the system-under-test and then act...

handlerMock.Verify();
```

When using richardszalay/mockhttp the following achieves the same and is more readable:

```C#
MockHttpMessageHandler handlerMock = new();
handlerMock
    .Expect("/api/v1/session/logout")
    .Respond(HttpStatusCode.OK);
HttpClient client = handlerMock.ToHttpClient();

// Inject 'client' to the system-under-test and then act...

handlerMock.VerifyNoOutstandingExpectation()
```
