using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;

namespace API.IntegrationTests.Extensions;

public static class WalletServiceClientExtensions
{
    public static async Task<IList<GranularCertificate>> RepeatedlyQueryCertificatesUntil(this IWalletClient client, Func<IEnumerable<GranularCertificate>, bool> condition, string ownerId, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(60);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.GetGranularCertificatesAsync(Guid.Parse(ownerId), CancellationToken.None, null);

            if (condition(response!.Result))
                return response.Result.ToList();

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for certificates in wallet not met within time limit ({limit.TotalSeconds} seconds)");
    }
}
