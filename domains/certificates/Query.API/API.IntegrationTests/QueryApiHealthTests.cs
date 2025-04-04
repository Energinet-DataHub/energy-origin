using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class QueryApiHealthTests : TestBase
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly IntegrationTestFixture _integrationTestFixture;

    public QueryApiHealthTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
        _integrationTestFixture = integrationTestFixture;
    }

    [Fact]
    public async Task Health_IsCalled_ReturnsOk()
    {
        using var client = factory.CreateClient();

        using var healthResponse1 = await client.RepeatedlyQueryUntil("/health",
            response => response.StatusCode == HttpStatusCode.OK);
    }
}

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> RepeatedlyQueryUntil(this HttpClient client, string url, Func<HttpResponseMessage, bool> condition, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.GetAsync(url);

            if (condition(response))
                return response;

            await Task.Delay(TimeSpan.FromMilliseconds(30));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for the request not met within time limit ({limit.TotalSeconds} seconds)");

    }

}
