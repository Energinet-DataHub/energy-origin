using Grpc.Core;
using ProjectOrigin.WalletSystem.V1;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace API.IntegrationTests.Extensions;

public static class WalletServiceClientExtensions
{
    public static async Task<QueryResponse> RepeatedlyQueryCertificatesUntil(this WalletService.WalletServiceClient client, Metadata metadata, Func<QueryResponse, bool> condition, TimeSpan? timeLimit = null)
    {
        if (timeLimit.HasValue && timeLimit.Value <= TimeSpan.Zero)
            throw new ArgumentException($"{nameof(timeLimit)} must be a positive time span");

        var limit = timeLimit ?? TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var response = await client.QueryGranularCertificatesAsync(new QueryRequest(), metadata);

            if (condition(response))
                return response;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception(
            $"Condition for certificates in wallet not met within time limit ({limit.TotalSeconds} seconds)");
    }
}
