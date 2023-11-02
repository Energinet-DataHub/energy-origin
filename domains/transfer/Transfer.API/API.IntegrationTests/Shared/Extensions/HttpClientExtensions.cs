using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace API.IntegrationTests.Shared.Extensions;

public static class HttpClientExtensions
{
    public static async Task<T> RepeatedlyGetUntil<T>(this HttpClient client, string requestUri, Func<T, bool> condition, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            using (var response = await client.GetAsync(requestUri))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadFromJsonAsync<T>(JsonDefault.Options);
                    if (content != null && condition(content))
                        return content;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for uri '{requestUri}' not met within time limit ({limit.TotalSeconds} seconds)");
    }
}
