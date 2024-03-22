using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests.Extensions;

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

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for the request not met within time limit ({limit.TotalSeconds} seconds)");

    }

}
