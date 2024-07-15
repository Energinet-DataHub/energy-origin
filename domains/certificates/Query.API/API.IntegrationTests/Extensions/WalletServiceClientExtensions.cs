using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectOriginClients.Models;

namespace API.IntegrationTests.Extensions;

public static class WalletServiceClientExtensions
{
    public const string WalletOwnerHeader = "wallet-owner";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public static async Task<IEnumerable<GranularCertificate>> QueryCertificates(this HttpClient client)
    {
        var response = await client.GetFromJsonAsync<ResultList<GranularCertificate>>("v1/certificates",
            options: JsonSerializerOptions);

        return response!.Result.ToList();
    }

    public static async Task<IList<GranularCertificate>> RepeatedlyQueryCertificatesUntil(this HttpClient client, Func<IEnumerable<GranularCertificate>, bool> condition, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.GetFromJsonAsync<ResultList<GranularCertificate>>("v1/certificates",
                options: JsonSerializerOptions);

            if (condition(response!.Result))
                return response.Result.ToList();

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for certificates in wallet not met within time limit ({limit.TotalSeconds} seconds)");
    }
}
