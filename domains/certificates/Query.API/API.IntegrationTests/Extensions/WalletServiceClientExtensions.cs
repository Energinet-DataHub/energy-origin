using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;

namespace API.IntegrationTests.Extensions;

public static class WalletServiceClientExtensions
{
    public const string WalletOwnerHeader = "wallet-owner";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public static async Task<IList<GranularCertificate>> RepeatedlyQueryCertificatesUntil(this IWalletClient client, Func<IEnumerable<GranularCertificate>, bool> condition, string ownerId, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(60);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.GetGranularCertificates(Guid.Parse(ownerId), CancellationToken.None, null);

            if (condition(response!.Result))
                return response.Result.ToList();

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for certificates in wallet not met within time limit ({limit.TotalSeconds} seconds)");
    }
}
