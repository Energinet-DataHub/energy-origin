using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Factories;
using API.UnitTests;
using EnergyOrigin.Domain.ValueObjects;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace API.IntegrationTests.Setup.Fixtures;

public class TestCaseContext : IAsyncDisposable
{
    private readonly Activity _activity;
    private readonly WireMockServer _wireMock;

    public HttpClient Client { get; }
    public OrganizationId OrganizationId { get; }
    public Tin Tin { get; }

    public TestCaseContext(
        TransferAgreementsApiWebApplicationFactory factory,
        WireMockServer wireMock,
        string testName)
    {
        _wireMock = wireMock;
        _activity = new Activity("TestCase").AddTag("TestMethod", testName).Start();

        var sub = Guid.NewGuid();
        OrganizationId = Any.OrganizationId();
        Tin = Tin.Create("12345678");

        Client = factory.CreateB2CAuthenticatedClient(sub, OrganizationId.Value, Tin.Value);
        Client.DefaultRequestHeaders.Add("traceparent", _activity.Id);
    }

    public void StubRequest(Action<IRequestBuilder> configureRequest, Action<IResponseBuilder> configureResponse)
    {
        var request = Request.Create().WithHeader("traceparent", $"*{_activity.TraceId}*");
        configureRequest(request);

        var response = Response.Create();
        configureResponse(response);

        _wireMock.Given(request).RespondWith(response);
    }

    public ValueTask DisposeAsync()
    {
        _activity.Stop();
        _activity.Dispose();
        return default;
    }
}

